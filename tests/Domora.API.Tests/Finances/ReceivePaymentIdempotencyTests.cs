using System.Net;
using System.Net.Http.Json;
using Domora.API.Tests.Infrastructure;
using Domora.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Domora.API.Tests.Finances;

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

        // Generate isolated user, organization, and owner membership data records
        var user = await TestData.CreateUserAsync(dbContext);
        var organization = await TestData.CreateOrganizationAsync(dbContext);
        await TestData.AddMembershipAsync(dbContext, user.Id, organization.Id);

        // Instantiated the client channel with authenticated token contexts attached
        using var client = AuthenticatedClientFactory.Create(_factory, user.Id);
        
        // Inject the required multi-tenant routing interceptor boundary header parameter
        client.DefaultRequestHeaders.Add("X-Organization-Id", organization.Id.ToString());

        // Create a unique natural business reference code block configuration
        var uniqueReference = $"MPESA-{Guid.NewGuid():N}";
        var paymentRequestPayload = new
        {
            Amount = 15000.00m,
            Currency = "KES",
            Reference = uniqueReference
        };


        // Act - Fire Request 1 (Simulating the first button submission pass)
        var firstResponse = await client.PostAsJsonAsync("api/finances/payments", paymentRequestPayload);
        
        // Assert Request 1 passes the entry pipeline cleanly
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        
        var firstPayment = await firstResponse.Content.ReadFromJsonAsync<PaymentTestResponse>();
        Assert.NotNull(firstPayment);

        // Act - Fire Request 2 (Simulating the immediate double-click retry loop)
        var secondResponse = await client.PostAsJsonAsync("api/finances/payments", paymentRequestPayload);

        // 4. Assert - Validate Idempotency Guard Invariants
        // Instead of a 409 or 500 error, the gate interceptor returns a 200 OK or 201 Created safely
        Assert.True(secondResponse.StatusCode == HttpStatusCode.OK || secondResponse.StatusCode == HttpStatusCode.Created);

        var secondPayment = await secondResponse.Content.ReadFromJsonAsync<PaymentTestResponse>();
        Assert.NotNull(secondPayment);

        // THE TRIUMPH: Both requests point to the exact same database tracking ID row!
        Assert.Equal(firstPayment!.Id, secondPayment!.Id);
        Assert.Equal(uniqueReference, secondPayment.Reference);
    }

    private sealed record PaymentTestResponse(
        Guid Id,
        decimal Amount,
        string Currency,
        string Reference
    );
}
