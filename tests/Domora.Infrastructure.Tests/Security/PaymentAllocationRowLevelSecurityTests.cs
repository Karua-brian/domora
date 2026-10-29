
using Domora.Application.Common.Context;
using Domora.Domain.Common;
using Domora.Domain.Finance;
using Domora.Domain.Finance.Enums;
using Domora.Domain.Leasing;
using Domora.Domain.Organizations;
using Domora.Domain.Organizations.ValueObjects;
using Domora.Domain.Properties;
using Domora.Domain.Properties.ValueObjects;
using Domora.Domain.Units;
using Domora.Domain.Units.Enums;
using Domora.Domain.Units.ValueObjects;
using Domora.Infrastructure.Persistence;
using Domora.Infrastructure.Persistence.Interceptors;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace Domora.Infrastructure.Tests.Security;

public sealed class PaymentAllocationRowLevelSecurityTests
{
    private readonly string _connectionString =
        Environment.GetEnvironmentVariable("DomoraTest")
        ?? throw new InvalidOperationException(
            "DomoraTest connection string is not configured."
        );

    public sealed class TestOrganizationContext : IOrganizationContext
    {
        public Guid OrganizationId { get; private set; }

        public TestOrganizationContext(Guid organizationId)
        {
            OrganizationId = organizationId;
        }

        public void Set(Guid organizationId)
        {
            OrganizationId = organizationId;
        }
    }

    private DomoraDbContext CreateContext(Guid organizationId)
    {
        var organizationContext =
            new TestOrganizationContext(organizationId);

        var interceptor =
            new OrganizationTransactionInterceptor(organizationContext);

        var options = new DbContextOptionsBuilder<DomoraDbContext>()
            .UseNpgsql(_connectionString)
            .AddInterceptors(interceptor)
            .Options;

        return new DomoraDbContext(options);
    }

    [Fact]
    public async Task Organization_should_read_its_own_payment_allocation()
    {
        var seeded = await SeedAllocationAsync();

        await using var context = CreateContext(seeded.OrganizationId);
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var result = await context.Set<PaymentAllocation>()
            .SingleOrDefaultAsync(x => x.Id == seeded.AllocationId);

        Assert.NotNull(result);
        Assert.Equal(seeded.OrganizationId, result.OrganizationId);
    }

    [Fact]
    public async Task Organization_should_not_read_another_organizations_allocation()
    {
        var seeded = await SeedAllocationAsync();

        await using var context = CreateContext(Guid.NewGuid());
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var result = await context.Set<PaymentAllocation>()
            .SingleOrDefaultAsync(x => x.Id == seeded.AllocationId);

        Assert.Null(result);
    }

    [Fact]
    public async Task Organization_should_not_insert_allocation_for_another_organization()
    {
        var seeded = await SeedAllocationAsync();

        // The database records belong to seeded.OrganizationId,
        // but this request executes as a different organization.
        var allocation = PaymentAllocation.Allocate(
            seeded.OrganizationId,
            seeded.PaymentId,
            seeded.InvoiceId,
            new Money(100m, "KES")
        );

        await using var context = CreateContext(Guid.NewGuid());
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        await context.Set<PaymentAllocation>().AddAsync(allocation);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync()
        );
    }

    [Fact]
    public async Task Database_should_reject_allocation_linking_payment_and_invoice_from_different_organizations()
    {
        var paymentSide = await SeedAllocationAsync();
        var otherInvoice = await SeedInvoiceAsync();

        // The allocation claims the payment's organization.
        // Its payment belongs to that organization, but its invoice does not.
        var invalidAllocation = PaymentAllocation.Allocate(
            paymentSide.OrganizationId,
            paymentSide.PaymentId,
            otherInvoice.InvoiceId,
            new Money(100m, "KES")
        );

        await using var context =
            CreateContext(paymentSide.OrganizationId);

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        await context.Set<PaymentAllocation>().AddAsync(invalidAllocation);

        // The composite foreign key should reject the mismatched invoice.
        await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync()
        );
    }

    [Fact]
    public async Task Organization_should_not_delete_another_organizations_allocation()
    {
        var seeded = await SeedAllocationAsync();

        await using var context = CreateContext(Guid.NewGuid());
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var affectedRows = await context.Set<PaymentAllocation>()
            .Where(x => x.Id == seeded.AllocationId)
            .ExecuteDeleteAsync();

        // This test checks that a different organization cannot
        // affect the protected row through a bulk operation.
        Assert.Equal(0, affectedRows);
    }

    private async Task<(
        Guid OrganizationId,
        Guid PaymentId,
        Guid InvoiceId,
        Guid AllocationId
    )> SeedAllocationAsync()
    {
        var invoice = await SeedInvoiceAsync();

        var payment = Payment.Receive(
            invoice.OrganizationId,
            new Money(5_000m, "KES"),
            $"ALLOCATION-{Guid.NewGuid():N}"
        );

        var allocation = PaymentAllocation.Allocate(
            invoice.OrganizationId,
            payment.Id,
            invoice.InvoiceId,
            new Money(1_000m, "KES")
        );

        await using var context =
            CreateContext(invoice.OrganizationId);

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        await context.Set<Payment>().AddAsync(payment);
        await context.Set<PaymentAllocation>().AddAsync(allocation);

        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        return (
            invoice.OrganizationId,
            payment.Id,
            invoice.InvoiceId,
            allocation.Id
        );
    }

    private async Task<(Guid OrganizationId, Guid InvoiceId)>
        SeedInvoiceAsync()
    {
        var organization = Organization.Register(
            OrganizationName.Create($"Allocation Org {Guid.NewGuid():N}")
        );

        var property = Property.Register(
            organization.Id,
            PropertyName.Create($"Property {Guid.NewGuid():N}")
        );

        var unit = Unit.Register(
            property.Id,
            UnitNumber.Create($"U-{Guid.NewGuid():N}"),
            UnitType.Apartment
        );

        var lease = Lease.Register(
            unit.Id,
            Guid.NewGuid(),
            new Money(10_000m, "KES")
        );

        var invoice = Invoice.Create(
            organization.Id,
            lease.Id,
            InvoiceType.Rent,
            new Money(10_000m, "KES"),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7))
        );

        await using var context = CreateContext(organization.Id);
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        await context.Set<Organization>().AddAsync(organization);
        await context.Set<Property>().AddAsync(property);
        await context.Set<Unit>().AddAsync(unit);
        await context.Set<Lease>().AddAsync(lease);
        await context.Set<Invoice>().AddAsync(invoice);

        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        return (organization.Id, invoice.Id);
    }
}
