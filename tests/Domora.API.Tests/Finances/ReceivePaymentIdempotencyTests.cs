using System.Net;
using System.Net.Http.Json;
using Domora.API.Tests.Infrastructure;
using Domora.Application.Common.Context;
using Domora.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Domora.API.Tests.Finances;

[Collection("Domora API database")]
public sealed class ReceivePaymentIdempotencyTests : IClassFixture<DomoraWebApplicationFactory>
{
    private readonly DomoraWebApplicationFactory _factory;

    public ReceivePaymentIdempotencyTests(
        DomoraWebApplicationFactory factory
    )
    {
        _factory = factory;
    }

    [Fact]
    public async Task Double_submitting_same_payment_reference_should_return_original_payment_safely()
    {
        // Arrange 
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DomoraDbContext>();

        var user = await TestData.CreateUserAsync(dbContext);
        var organization = await TestData.CreateOrganizationAsync(dbContext, user.Id);

        using var client = AuthenticatedClientFactory.Create(_factory, user.Id);
        
        client.DefaultRequestHeaders.Add(
            "X-Organization-Id", 
            organization.Id.ToString()
        );

        // Create a unique natural business reference code block configuration
        var uniqueReference = $"MPESA-{Guid.NewGuid():N}";
        var paymentRequestPayload = new
        {
            Amount = 15000.00m,
            Currency = "KES",
            Reference = uniqueReference
        };


        // Act - Fire Request 1 (Simulating the first button submission pass)
        var firstResponse = await client.PostAsJsonAsync(
            "api/finances/payments", 
            paymentRequestPayload
        );
        
        var responseBody = await firstResponse.Content.ReadAsStringAsync();

        Assert.True(
            firstResponse.StatusCode == HttpStatusCode.Created,
            $"Expected Created but received {(int)firstResponse.StatusCode} " +
            $"({firstResponse.StatusCode}). Response: {responseBody}"
        );
        // // Assert Request 1 passes the entry pipeline cleanly
        // Assert.Equal(
        //     HttpStatusCode.Created, 
        //     firstResponse.StatusCode
        // );
        
        var firstPayment = await firstResponse.Content
            .ReadFromJsonAsync<PaymentTestResponse>();

        Assert.NotNull(firstPayment);

        // Act - Fire Request 2 (Simulating the immediate double-click retry loop)
        var secondResponse = await client.PostAsJsonAsync(
            "api/finances/payments", 
            paymentRequestPayload
        );

        // 4. Assert - Validate Idempotency Guard Invariants
        // Instead of a 409 or 500 error, the gate interceptor returns a 200 OK or 201 Created safely
        Assert.True(
            secondResponse.StatusCode == HttpStatusCode.OK || 
            secondResponse.StatusCode == HttpStatusCode.Created
        );

        var secondPayment = await secondResponse.Content
            .ReadFromJsonAsync<PaymentTestResponse>();

        Assert.NotNull(secondPayment);

        // THE TRIUMPH: Both requests point to the exact same database tracking ID row!
        Assert.Equal(
            firstPayment!.Id, 
            secondPayment!.Id
        );

        Assert.Equal(
            uniqueReference, 
            secondPayment.Reference
        );
    }

    [Fact]
    public async Task Reusing_reference_with_different_amount_or_currency_returns_conflict()
    {
        var (client, reference) = await CreateClientAndReferenceAsync();
        using (client)
        {
            var first = await client.PostAsJsonAsync("api/finances/payments", new
            {
                amount = 150m,
                currency = "KES",
                reference
            });
            var differentAmount = await client.PostAsJsonAsync("api/finances/payments", new
            {
                amount = 151m,
                currency = "KES",
                reference
            });
            var differentCurrency = await client.PostAsJsonAsync("api/finances/payments", new
            {
                amount = 150m,
                currency = "USD",
                reference
            });

            Assert.Equal(HttpStatusCode.Created, first.StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, differentAmount.StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, differentCurrency.StatusCode);
        }
    }

    [Fact]
    public async Task Concurrent_identical_payment_retries_return_one_existing_payment()
    {
        var (client, reference) = await CreateClientAndReferenceAsync();
        using (client)
        {
            var payload = new { amount = 150m, currency = "KES", reference };
            var responses = await Task.WhenAll(
                client.PostAsJsonAsync("api/finances/payments", payload),
                client.PostAsJsonAsync("api/finances/payments", payload));

            Assert.All(responses, response => Assert.Contains(
                response.StatusCode, new[] { HttpStatusCode.Created, HttpStatusCode.OK }));

            var payments = await Task.WhenAll(responses.Select(response =>
                response.Content.ReadFromJsonAsync<PaymentTestResponse>()));
            Assert.NotNull(payments[0]);
            Assert.NotNull(payments[1]);
            Assert.Equal(payments[0]!.Id, payments[1]!.Id);
        }
    }

    private async Task<(HttpClient Client, string Reference)> CreateClientAndReferenceAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DomoraDbContext>();
        var user = await TestData.CreateUserAsync(db);
        var organization = await TestData.CreateOrganizationAsync(db, user.Id);
        var client = AuthenticatedClientFactory.Create(_factory, user.Id);
        client.DefaultRequestHeaders.Add("X-Organization-Id", organization.Id.ToString());
        return (client, $"SAFETY-{Guid.NewGuid():N}");
    }

    private sealed record PaymentTestResponse(
        Guid Id,
        decimal Amount,
        string Currency,
        string Reference
    );
}
