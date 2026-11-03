using Domora.Domain.Common;
using Domora.Domain.Finance;
using Domora.Domain.Finance.Enums;
using Domora.Domain.Leasing;
using Domora.Domain.Properties;
using Domora.Domain.Properties.ValueObjects;
using Domora.Domain.Units;
using Domora.Domain.Units.Enums;
using Domora.Domain.Units.ValueObjects;
using Domora.Infrastructure.Persistence;
using Domora.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Domora.Infrastructure.Tests.Finance;

public sealed class FinancialVisibilityReadRepositoryTests
{
    [Fact]
    public async Task Invoice_balances_use_only_active_payment_allocations_and_scope_by_organization()
    {
        var options = new DbContextOptionsBuilder<DomoraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new DomoraDbContext(options);

        var organizationId = Guid.NewGuid();
        var otherOrganizationId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var property = Property.Register(organizationId, PropertyName.Create("Property"));
        var unit = Unit.Register(property.Id, UnitNumber.Create("101"), UnitType.Apartment);
        var lease = Lease.Register(unit.Id, tenantId, new Money(100m, "KES"));
        var unpaid = Invoice.Create(organizationId, lease.Id, InvoiceType.Rent,
            new Money(100m, "KES"), new DateOnly(2026, 1, 1));
        var partial = Invoice.Create(organizationId, lease.Id, InvoiceType.Garbage,
            new Money(200m, "KES"), new DateOnly(2026, 1, 2));
        var paid = Invoice.Create(organizationId, lease.Id, InvoiceType.Water,
            new Money(300m, "KES"), new DateOnly(2026, 1, 3));
        var voidedAllocation = Invoice.Create(organizationId, lease.Id, InvoiceType.Garbage,
            new Money(80m, "KES"), new DateOnly(2026, 1, 4));
        var crossOrganizationInvoice = Invoice.Create(otherOrganizationId, lease.Id,
            InvoiceType.Rent, new Money(900m, "KES"), new DateOnly(2026, 1, 5));

        var activePayment = Payment.Receive(organizationId, new Money(500m, "KES"), "active");
        var voidedPayment = Payment.Receive(organizationId, new Money(50m, "KES"), "voided");
        voidedPayment.Void();

        await db.Properties.AddAsync(property);
        await db.Units.AddAsync(unit);
        await db.Leases.AddAsync(lease);
        await db.Invoices.AddRangeAsync(unpaid, partial, paid, voidedAllocation, crossOrganizationInvoice);
        await db.Payments.AddRangeAsync(activePayment, voidedPayment);
        await db.PaymentAllocations.AddRangeAsync(
            PaymentAllocation.Allocate(organizationId, activePayment.Id, partial.Id, new Money(75m, "KES")),
            PaymentAllocation.Allocate(organizationId, activePayment.Id, paid.Id, new Money(300m, "KES")),
            PaymentAllocation.Allocate(organizationId, voidedPayment.Id, voidedAllocation.Id, new Money(50m, "KES")));
        await db.SaveChangesAsync();

        var results = await new FinancialVisibilityReadRepository(db)
            .GetInvoicesAsync(organizationId, CancellationToken.None);

        Assert.Equal(4, results.Count);
        Assert.Equal(100m, Assert.Single(results, x => x.InvoiceId == unpaid.Id).OutstandingBalance);
        Assert.Equal(125m, Assert.Single(results, x => x.InvoiceId == partial.Id).OutstandingBalance);
        Assert.Equal(0m, Assert.Single(results, x => x.InvoiceId == paid.Id).OutstandingBalance);
        Assert.Equal(80m, Assert.Single(results, x => x.InvoiceId == voidedAllocation.Id).OutstandingBalance);
        Assert.DoesNotContain(results, x => x.InvoiceId == crossOrganizationInvoice.Id);
    }
}
