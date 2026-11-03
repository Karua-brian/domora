using System.Net;
using System.Net.Http.Json;
using Domora.API.Tests.Infrastructure;
using Domora.Application.Common.Context;
using Domora.Domain.Common;
using Domora.Domain.Finance;
using Domora.Domain.Finance.Enums;
using Domora.Domain.Leasing;
using Domora.Domain.Organizations;
using Domora.Domain.Organizations.Enums;
using Domora.Domain.Properties;
using Domora.Domain.Properties.ValueObjects;
using Domora.Domain.Units;
using Domora.Domain.Units.Enums;
using Domora.Domain.Units.ValueObjects;
using Domora.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Domora.API.Tests.Finances;

[Collection("Domora API database")]
public sealed class PaymentAllocationReversalApiTests : IClassFixture<DomoraWebApplicationFactory>
{
    private readonly DomoraWebApplicationFactory _factory;
    public PaymentAllocationReversalApiTests(DomoraWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Owner_can_reverse_and_balances_update_while_finance_and_member_are_denied()
    {
        Guid orgId, ownerId, financeId, memberId, allocationId, paymentId, invoiceId;
        string currency = "KES";
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DomoraDbContext>();
            var owner = await TestData.CreateUserAsync(db);
            var finance = await TestData.CreateUserAsync(db);
            var member = await TestData.CreateUserAsync(db);
            var organization = await TestData.CreateOrganizationAsync(db, owner.Id);
            await TestData.AddMembershipAsync(db, finance.Id, organization.Id, OrganizationRole.Finance);
            await TestData.AddMembershipAsync(db, member.Id, organization.Id, OrganizationRole.Member);
            var property = Property.Register(organization.Id, PropertyName.Create($"Reversal {Guid.NewGuid():N}"));
            var unit = Unit.Register(property.Id, UnitNumber.Create($"R-{Guid.NewGuid():N}"), UnitType.Apartment);
            var lease = Lease.Register(unit.Id, Guid.NewGuid(), new Money(100m, currency));
            var invoice = Invoice.Create(organization.Id, lease.Id, InvoiceType.Rent,
                new Money(100m, currency), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)));
            invoice.RecalculatePaymentStatus(new Money(100m, currency));
            var payment = Payment.Receive(organization.Id, new Money(100m, currency), $"REV-{Guid.NewGuid():N}");
            payment.DeductCredit(new Money(100m, currency));
            var allocation = PaymentAllocation.Allocate(organization.Id, payment.Id, invoice.Id, new Money(100m, currency));
            scope.ServiceProvider.GetRequiredService<IOrganizationContext>().Set(organization.Id);
            await using var transaction = await db.Database.BeginTransactionAsync();
            await db.Properties.AddAsync(property);
            await db.Units.AddAsync(unit);
            await db.Leases.AddAsync(lease);
            await db.Invoices.AddAsync(invoice);
            await db.Payments.AddAsync(payment);
            await db.PaymentAllocations.AddAsync(allocation);
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            orgId = organization.Id; ownerId = owner.Id; financeId = finance.Id; memberId = member.Id;
            allocationId = allocation.Id; paymentId = payment.Id; invoiceId = invoice.Id;
        }

        using var ownerClient = Client(ownerId, orgId);
        using var financeClient = Client(financeId, orgId);
        using var memberClient = Client(memberId, orgId);
        var path = $"/api/finances/payment-allocations/{allocationId}/reverse";
        Assert.Equal(HttpStatusCode.Forbidden, (await financeClient.PostAsJsonAsync(path, new { reason = "wrong invoice" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await memberClient.PostAsJsonAsync(path, new { reason = "wrong invoice" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await ownerClient.PostAsJsonAsync(path, new { reason = " " })).StatusCode);
        var concurrent = await Task.WhenAll(
            ownerClient.PostAsJsonAsync(path, new { reason = "Wrong invoice" }),
            ownerClient.PostAsJsonAsync(path, new { reason = "Wrong invoice retry" }));
        Assert.Single(concurrent, x => x.StatusCode == HttpStatusCode.OK);
        Assert.Single(concurrent, x => x.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal(HttpStatusCode.Conflict, (await ownerClient.PostAsJsonAsync(path, new { reason = "duplicate" })).StatusCode);

        var invoiceRows = await ownerClient.GetFromJsonAsync<List<InvoiceBalanceApiModel>>("/api/finances/invoices");
        var invoiceRow = Assert.Single(invoiceRows!, x => x.InvoiceId == invoiceId);
        Assert.Equal(0m, invoiceRow.AllocatedAmount);
        Assert.Equal(100m, invoiceRow.OutstandingBalance);
        var tenantRows = await ownerClient.GetFromJsonAsync<List<TenantBalanceApiModel>>("/api/finances/tenant-balances");
        Assert.Contains(tenantRows!, x => x.OutstandingBalance == 100m && x.Currency == currency);
        var arrearsRows = await ownerClient.GetFromJsonAsync<List<InvoiceBalanceApiModel>>("/api/finances/arrears");
        Assert.Contains(arrearsRows!, x => x.InvoiceId == invoiceId && x.OutstandingBalance == 100m);

        await using var verifyScope = _factory.Services.CreateAsyncScope();
        verifyScope.ServiceProvider.GetRequiredService<IOrganizationContext>().Set(orgId);
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<DomoraDbContext>();
        await using var verifyTransaction = await verifyDb.Database.BeginTransactionAsync();
        Assert.Single(await verifyDb.PaymentAllocations.Where(x => x.Id == allocationId).ToListAsync());
        Assert.Single(await verifyDb.PaymentAllocationReversals.Where(x => x.PaymentAllocationId == allocationId).ToListAsync());
        Assert.Equal(100m, (await verifyDb.Payments.SingleAsync(x => x.Id == paymentId)).UnallocatedAmount.Amount);
        Assert.Equal(InvoiceStatus.Pending, (await verifyDb.Invoices.SingleAsync(x => x.Id == invoiceId)).Status);
        await verifyTransaction.CommitAsync();

        var otherOrganizationId = Guid.NewGuid();
        verifyScope.ServiceProvider.GetRequiredService<IOrganizationContext>().Set(otherOrganizationId);
        await using var otherOrganizationTransaction = await verifyDb.Database.BeginTransactionAsync();
        Assert.Empty(await verifyDb.PaymentAllocations.Where(x => x.Id == allocationId).ToListAsync());
        Assert.Empty(await verifyDb.PaymentAllocationReversals.Where(x => x.PaymentAllocationId == allocationId).ToListAsync());
    }

    private HttpClient Client(Guid userId, Guid organizationId)
    {
        var client = AuthenticatedClientFactory.Create(_factory, userId);
        client.DefaultRequestHeaders.Add("X-Organization-Id", organizationId.ToString());
        return client;
    }

    private sealed record InvoiceBalanceApiModel(Guid InvoiceId, decimal AllocatedAmount, decimal OutstandingBalance);
    private sealed record TenantBalanceApiModel(Guid TenantId, string Currency, decimal OutstandingBalance);
}
