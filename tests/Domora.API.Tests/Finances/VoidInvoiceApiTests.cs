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
using Npgsql;

namespace Domora.API.Tests.Finances;

[Collection("Domora API database")]
public sealed class VoidInvoiceApiTests : IClassFixture<DomoraWebApplicationFactory>
{
    private readonly DomoraWebApplicationFactory _factory;
    public VoidInvoiceApiTests(DomoraWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Owner_can_void_unpaid_invoice_and_visibility_and_allocation_exclude_it()
    {
        Guid organizationId, ownerId, financeId, memberId, invoiceId, foreignInvoiceId, leaseId, paymentId;
        var tenantId = Guid.NewGuid();
        await using (var seedScope = _factory.Services.CreateAsyncScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<DomoraDbContext>();
            var owner = await TestData.CreateUserAsync(db);
            var finance = await TestData.CreateUserAsync(db);
            var member = await TestData.CreateUserAsync(db);
            var foreignOwner = await TestData.CreateUserAsync(db);
            var organization = await TestData.CreateOrganizationAsync(db, owner.Id);
            var foreignOrganization = await TestData.CreateOrganizationAsync(db, foreignOwner.Id);
            await TestData.AddMembershipAsync(db, finance.Id, organization.Id, OrganizationRole.Finance);
            await TestData.AddMembershipAsync(db, member.Id, organization.Id, OrganizationRole.Member);
            var property = Property.Register(organization.Id, PropertyName.Create($"Invoice void {Guid.NewGuid():N}"));
            var unit = Unit.Register(property.Id, UnitNumber.Create($"IV-{Guid.NewGuid():N}"), UnitType.Apartment);
            var lease = Lease.Register(unit.Id, tenantId, new Money(100m, "KES"));
            var invoice = Invoice.Create(organization.Id, lease.Id, InvoiceType.Rent,
                new Money(100m, "KES"), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)));
            var payment = Payment.Receive(organization.Id, new Money(50m, "KES"), $"VOID-INV-{Guid.NewGuid():N}");
            seedScope.ServiceProvider.GetRequiredService<IOrganizationContext>().Set(organization.Id);
            await using var seedTransaction = await db.Database.BeginTransactionAsync();
            await db.Properties.AddAsync(property);
            await db.Units.AddAsync(unit);
            await db.Leases.AddAsync(lease);
            await db.Invoices.AddAsync(invoice);
            await db.Payments.AddAsync(payment);
            await db.SaveChangesAsync();
            await seedTransaction.CommitAsync();
            var foreignProperty = Property.Register(foreignOrganization.Id,
                PropertyName.Create($"Foreign invoice void {Guid.NewGuid():N}"));
            var foreignUnit = Unit.Register(foreignProperty.Id,
                UnitNumber.Create($"FV-{Guid.NewGuid():N}"), UnitType.Apartment);
            var foreignLease = Lease.Register(foreignUnit.Id, Guid.NewGuid(), new Money(100m, "KES"));
            var foreignInvoice = Invoice.Create(foreignOrganization.Id, foreignLease.Id, InvoiceType.Rent,
                new Money(100m, "KES"), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)));
            seedScope.ServiceProvider.GetRequiredService<IOrganizationContext>().Set(foreignOrganization.Id);
            await using (var foreignTransaction = await db.Database.BeginTransactionAsync())
            {
                await db.Properties.AddAsync(foreignProperty);
                await db.Units.AddAsync(foreignUnit);
                await db.Leases.AddAsync(foreignLease);
                await db.Invoices.AddAsync(foreignInvoice);
                await db.SaveChangesAsync();
                await foreignTransaction.CommitAsync();
            }
            organizationId = organization.Id; ownerId = owner.Id; financeId = finance.Id; memberId = member.Id;
            invoiceId = invoice.Id; foreignInvoiceId = foreignInvoice.Id; leaseId = lease.Id; paymentId = payment.Id;
        }

        using var ownerClient = Client(ownerId, organizationId);
        using var financeClient = Client(financeId, organizationId);
        using var memberClient = Client(memberId, organizationId);
        var path = $"/api/finances/invoices/{invoiceId}/void";
        Assert.Equal(HttpStatusCode.BadRequest,
            (await ownerClient.PostAsJsonAsync(path, new { reason = " " })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await financeClient.PostAsJsonAsync(path, new { reason = "finance denied" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await memberClient.PostAsJsonAsync(path, new { reason = "member denied" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await ownerClient.PostAsJsonAsync($"/api/finances/invoices/{foreignInvoiceId}/void",
                new { reason = "cross organization" })).StatusCode);

        var concurrentCancels = await Task.WhenAll(
            ownerClient.PostAsJsonAsync(path, new { reason = "Issued in error" }),
            ownerClient.PostAsJsonAsync(path, new { reason = "Issued in error retry" }));
        Assert.Single(concurrentCancels, x => x.StatusCode == HttpStatusCode.OK);
        Assert.Single(concurrentCancels, x => x.StatusCode == HttpStatusCode.Conflict);

        var invoices = await ownerClient.GetFromJsonAsync<List<InvoiceBalanceApiModel>>("/api/finances/invoices");
        var cancelledInvoice = Assert.Single(invoices!, x => x.InvoiceId == invoiceId);
        Assert.Equal("Voided", cancelledInvoice.Status);
        Assert.Equal(100m, cancelledInvoice.Amount);
        Assert.Equal(0m, cancelledInvoice.OutstandingBalance);
        Assert.Contains(cancelledInvoice.CancellationReason, new[] { "Issued in error", "Issued in error retry" });
        Assert.Equal(ownerId, cancelledInvoice.CancellationActorId);
        Assert.NotNull(cancelledInvoice.CancelledAtUtc);
        Assert.Empty(await ownerClient.GetFromJsonAsync<List<TenantBalanceApiModel>>("/api/finances/tenant-balances") ?? []);
        Assert.Empty(await ownerClient.GetFromJsonAsync<List<InvoiceBalanceApiModel>>("/api/finances/arrears") ?? []);

        var allocation = await ownerClient.PostAsJsonAsync("/api/finances/payment-allocations",
            new { paymentId, leaseId, amount = 50m, currency = "KES" });
        Assert.Equal(HttpStatusCode.Created, allocation.StatusCode);
        var allocationResponse = await allocation.Content.ReadFromJsonAsync<AllocateResponse>();
        Assert.NotNull(allocationResponse);
        Assert.Equal(0m, allocationResponse.TotalAllocated);
        Assert.Empty(allocationResponse.Allocations);

        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DomoraDbContext>();
        scope.ServiceProvider.GetRequiredService<IOrganizationContext>().Set(organizationId);
        await using var transaction = await context.Database.BeginTransactionAsync();
        Assert.Single(await context.Invoices.Where(x => x.Id == invoiceId).ToListAsync());
        var invoiceVoid = await context.InvoiceVoids.SingleAsync(x => x.InvoiceId == invoiceId);
        Assert.Contains(invoiceVoid.Reason, new[] { "Issued in error", "Issued in error retry" });
        Assert.Equal(0, await context.PaymentAllocations.CountAsync(x => x.InvoiceId == invoiceId));
        Assert.Equal(50m, (await context.Payments.SingleAsync(x => x.Id == paymentId)).UnallocatedAmount.Amount);

        await transaction.CommitAsync();
        scope.ServiceProvider.GetRequiredService<IOrganizationContext>().Set(Guid.NewGuid());
        await using var otherOrgTransaction = await context.Database.BeginTransactionAsync();
        Assert.Empty(await context.InvoiceVoids.Where(x => x.InvoiceId == invoiceId).ToListAsync());
        var rejectedInsertId = Guid.NewGuid();
        await Assert.ThrowsAsync<PostgresException>(() => context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "InvoiceVoids" ("Id", "OrganizationId", "InvoiceId", "ReplacementInvoiceId", "ActorUserId", "Reason", "VoidedAtUtc")
            VALUES ({rejectedInsertId}, {organizationId}, {invoiceId}, NULL, {ownerId}, {"cross organization write"}, {DateTimeOffset.UtcNow})
            """));
    }

    private HttpClient Client(Guid userId, Guid organizationId)
    {
        var client = AuthenticatedClientFactory.Create(_factory, userId);
        client.DefaultRequestHeaders.Add("X-Organization-Id", organizationId.ToString());
        return client;
    }

    private sealed record InvoiceBalanceApiModel(Guid InvoiceId, decimal Amount, string Status,
        decimal OutstandingBalance, string? CancellationReason, Guid? CancellationActorId, DateTimeOffset? CancelledAtUtc);
    private sealed record TenantBalanceApiModel(Guid TenantId, string Currency, decimal OutstandingBalance);
    private sealed record AllocateResponse(decimal TotalAllocated, IReadOnlyCollection<object> Allocations);
}
