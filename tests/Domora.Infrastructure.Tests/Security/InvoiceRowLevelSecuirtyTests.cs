
using Domora.Application.Common.Context;
using Domora.Domain.Common;
using Domora.Domain.Finance;
using Domora.Domain.Finance.Enums;
using Domora.Domain.Organizations;
using Domora.Domain.Properties;
using Domora.Domain.Leasing;
using Domora.Infrastructure.Persistence;
using Domora.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Domora.Domain.Organizations.ValueObjects;
using Domora.Domain.Properties.ValueObjects;
using Domora.Domain.Units;
using Domora.Domain.Units.ValueObjects;
using Domora.Domain.Units.Enums;

namespace Domora.Infrastructure.Tests.Security;

public sealed class InvoiceRowLevelSecurityTests
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
    public async Task Organization_should_read_its_own_invoice()
    {
        var seeded = await SeedInvoiceAsync();

        await using var context = CreateContext(seeded.OrganizationId);
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var result = await context.Set<Invoice>()
            .SingleOrDefaultAsync(i => i.Id == seeded.InvoiceId);

        Assert.NotNull(result);
        Assert.Equal(seeded.OrganizationId, result.OrganizationId);
    }

    [Fact]
    public async Task Organization_should_not_read_another_organizations_invoice()
    {
        var seeded = await SeedInvoiceAsync();
        var otherOrganizationId = Guid.NewGuid();

        await using var context = CreateContext(otherOrganizationId);
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var result = await context.Set<Invoice>()
            .SingleOrDefaultAsync(i => i.Id == seeded.InvoiceId);

        Assert.Null(result);
    }

    [Fact]
    public async Task Organization_should_not_insert_invoice_for_another_organization()
    {
        var seeded = await SeedInvoiceAsync();
        var actingOrganizationId = Guid.NewGuid();

        // The invoice claims an organization different from its lease owner.
        var unauthorizedInvoice = Invoice.Create(
            actingOrganizationId,
            seeded.LeaseId,
            InvoiceType.Rent,
            new Money(10_000, "KES"),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7))
        );

        await using var context = CreateContext(seeded.OrganizationId);
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        await context.Set<Invoice>().AddAsync(unauthorizedInvoice);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync()
        );
    }

    [Fact]
    public async Task Organization_should_not_update_another_organizations_invoice()
    {
        var seeded = await SeedInvoiceAsync();

        await using var context = CreateContext(Guid.NewGuid());
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var affectedRows = await context.Set<Invoice>()
            .Where(i => i.Id == seeded.InvoiceId)
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(
                    i => i.DueDate,
                    DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30))
                )
            );

        Assert.Equal(0, affectedRows);
    }

    [Fact]
    public async Task Organization_should_not_delete_another_organizations_invoice()
    {
        var seeded = await SeedInvoiceAsync();

        await using var context = CreateContext(Guid.NewGuid());
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var affectedRows = await context.Set<Invoice>()
            .Where(i => i.Id == seeded.InvoiceId)
            .ExecuteDeleteAsync();

        Assert.Equal(0, affectedRows);
    }

    private async Task<(Guid OrganizationId, Guid LeaseId, Guid InvoiceId)>
        SeedInvoiceAsync()
    {
        var organizationId = Guid.NewGuid();

        var organization = Organization.Register(
            OrganizationName.Create($"Invoice Test Org {Guid.NewGuid():N}")
        );

        // Use the ID generated by the organization aggregate.
        organizationId = organization.Id;

        var property = Property.Register(
            organizationId,
            PropertyName.Create($"Property {Guid.NewGuid():N}")
        );

        var unit = Unit.Register(
            property.Id,
            UnitNumber.Create($"U-{Guid.NewGuid():N}"),
            UnitType.Apartment
        );

        // No Tenant entity exists yet, so use a placeholder tenant ID.
        var lease = Lease.Register(
            unit.Id,
            Guid.NewGuid(),
            new Money(10_000, "KES")
        );

        var invoice = Invoice.Create(
            organizationId,
            lease.Id,
            InvoiceType.Rent,
            new Money(10_000, "KES"),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7))
        );

        await using var context = CreateContext(organizationId);
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        await context.Set<Organization>().AddAsync(organization);
        await context.Set<Property>().AddAsync(property);
        await context.Set<Unit>().AddAsync(unit);
        await context.Set<Lease>().AddAsync(lease);
        await context.Set<Invoice>().AddAsync(invoice);

        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        return (organizationId, lease.Id, invoice.Id);
    }
}
