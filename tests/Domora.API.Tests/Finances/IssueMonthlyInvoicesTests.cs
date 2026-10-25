using System.Net;
using System.Net.Http.Json;
using Domora.API.Tests.Infrastructure;
using Domora.Application.Common.Context;
using Domora.Domain.Common;
using Domora.Domain.Finance.Enums;
using Domora.Domain.Leasing;
using Domora.Domain.Properties;
using Domora.Domain.Properties.ValueObjects;
using Domora.Domain.Units.Enums;
using Domora.Domain.Units.ValueObjects;
using Domora.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Domora.API.Tests.Finances;

[Collection("Sequential")]
public sealed class IssueMonthlyInvoicesTests : IClassFixture<DomoraWebApplicationFactory>
{
    private readonly DomoraWebApplicationFactory _factory;

    public IssueMonthlyInvoicesTests(DomoraWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Trigger_monthly_billing_should_generate_rent_and_garbage_invoices_for_active_leases_without_duplication()
    {
        // 1. Arrange - Setup real multi-tenant data states inside PostgreSQL
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DomoraDbContext>();

        dbContext.ChangeTracker.Clear();

        var organizationContext = scope.ServiceProvider.GetRequiredService<IOrganizationContext>();

        // Generate tenant structural records
        var user = await TestData.CreateUserAsync(dbContext);
        var organization = await TestData.CreateOrganizationAsync(dbContext);
        await TestData.AddMembershipAsync(dbContext, user.Id, organization.Id);

        organizationContext.Set(organization.Id);

        // Map an active lease for this organization context sandbox
        var property = Property.Register(
            organization.Id,
            PropertyName.Create($"Property-{Guid.NewGuid():N}")
        );
        await dbContext.Properties.AddAsync(property);

        var unit = Domora.Domain.Units.Unit.Register(
            property.Id,
            UnitNumber.Create($"U-{Guid.NewGuid():N}"),
            UnitType.Apartment
        );
        await dbContext.Units.AddAsync(unit);

        var monthlyRent = new Money(15000.00m, "KES");
        var lease = Lease.Register(
            unit.Id, 
            Guid.NewGuid(), 
            monthlyRent
        );
        
        await dbContext.Leases.AddAsync(lease);
        await dbContext.SaveChangesAsync();

        // Instantiate authenticated client pipeline contexts
        using var client = AuthenticatedClientFactory.Create(_factory, user.Id);
        client.DefaultRequestHeaders.Add("X-Organization-Id", organization.Id.ToString());


        // 2. Act - Fire Simulation Request 1 (Warp time to November 1st, 2026)
        var targetSimulationDate = "2026-11-01";
        var firstResponse = await client.PostAsync(
            $"/api/test/trigger-monthly-billing?date={targetSimulationDate}", 
            null
        );
        
        // Assert the simulation endpoint returns HTTP 200 OK
        
        var responseBody = await firstResponse.Content.ReadAsStringAsync();

        Assert.True(
            firstResponse.StatusCode == HttpStatusCode.OK,
            $"Expected OK but received {(int)firstResponse.StatusCode} " +
            $"({firstResponse.StatusCode}). Response: {responseBody}"
        );

        var responseData = await firstResponse.Content.ReadFromJsonAsync<BillingSimulationResponse>();
        Assert.NotNull(responseData);
        Assert.True(
            responseData!.TotalInvoicesGenerated >= 2, 
            "Should have generated at least Rent and Garbage invoices."
        );


        // 3. Assert - Verify Data Integrity inside the Database
        // Clear EF Core tracking caches to force a direct, true read from PostgreSQL tables
        dbContext.ChangeTracker.Clear();   

        organizationContext.Set(organization.Id);

        await using (var transaction =
            await dbContext.Database.BeginTransactionAsync())
        {     

        var generatedInvoices = await dbContext.Invoices
            .Where(i => i.LeaseId == lease.Id)
            .ToListAsync();

        // Verify exactly two distinct utility invoices exist for this billing month cycle
        Assert.True(
            generatedInvoices.Count == 2,
            $"Expected 2 invoices but found {generatedInvoices.Count}. " +
            $"Types: {string.Join(", ", generatedInvoices.Select(i => i.Type))}"
        );
        // Assert.Equal(2, generatedInvoices.Count);
        
        var rentInvoice = generatedInvoices.Single(i => i.Type == InvoiceType.Rent);
        var garbageInvoice = generatedInvoices.Single(i => i.Type == InvoiceType.Garbage);

        Assert.Equal(15000.00m, rentInvoice.Amount.Amount);
        Assert.Equal(100.00m, garbageInvoice.Amount.Amount);
        Assert.Equal(InvoiceStatus.Pending, rentInvoice.Status);
        Assert.Equal(InvoiceStatus.Pending, garbageInvoice.Status);
        Assert.Equal(new DateOnly(2026, 11, 5), rentInvoice.DueDate); // Stamped due by the 5th!

        await transaction.CommitAsync();
        }        
        // 4. Act - Fire Request 2 (Simulate an accidental background double-run)
        var secondResponse = await client.PostAsync(
            $"/api/test/trigger-monthly-billing?date={targetSimulationDate}", 
            null
        );
        Assert.Equal(
            HttpStatusCode.OK, 
            secondResponse.StatusCode
        );

        var secondResponseData = await secondResponse.Content
            .ReadFromJsonAsync<BillingSimulationResponse>();
        Assert.NotNull(secondResponseData);
        
        // THE IDEMPOTENCY TRIUMPH: Zero new invoices are generated on the re-run pass!
        Assert.Equal(0, secondResponseData!.TotalInvoicesGenerated);

        await using (var transaction =
            await dbContext.Database.BeginTransactionAsync())
        {  

        var postRunInvoicesCount = 
            await dbContext.Invoices.CountAsync(i => i.LeaseId == lease.Id);

        Assert.Equal(2, postRunInvoicesCount); // Total invoice footprint remains strictly 2.

        await transaction.CommitAsync();
        }
    }

    private sealed record BillingSimulationResponse(
        int TotalOrganizationsProcessed,
        int TotalInvoicesGenerated
    );
}
