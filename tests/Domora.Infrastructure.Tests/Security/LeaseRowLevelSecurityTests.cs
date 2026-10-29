
using Domora.Application.Common.Context;
using Domora.Domain.Common;
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
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Domora.Infrastructure.Tests.Security;

public sealed class LeaseRowLevelSecurityTests
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
    public async Task Organization_should_read_its_own_lease()
    {
        var seeded = await SeedLeaseAsync();

        await using var context = CreateContext(seeded.OrganizationId);
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var result = await context.Set<Lease>()
            .SingleOrDefaultAsync(x => x.Id == seeded.LeaseId);

        Assert.NotNull(result);
        Assert.Equal(seeded.UnitId, result.UnitId);
    }

    [Fact]
    public async Task Organization_should_not_read_another_organizations_lease()
    {
        var seeded = await SeedLeaseAsync();

        await using var context = CreateContext(Guid.NewGuid());
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var result = await context.Set<Lease>()
            .SingleOrDefaultAsync(x => x.Id == seeded.LeaseId);

        Assert.Null(result);
    }

    [Fact]
    public async Task Organization_should_not_insert_lease_for_another_organizations_unit()
    {
        var foreignUnit = await SeedLeaseAsync();

        var invalidLease = Lease.Register(
            foreignUnit.UnitId,
            Guid.NewGuid(),
            new Money(10_000m, "KES")
        );

        await using var context = CreateContext(Guid.NewGuid());
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        await context.Set<Lease>().AddAsync(invalidLease);

        var exception = await Record.ExceptionAsync(
            () => context.SaveChangesAsync()
        );

        Assert.NotNull(exception);
        Assert.Equal("42501", FindPostgresException(exception)?.SqlState);
    }

    [Fact]
    public async Task Organization_should_not_reassign_its_lease_to_another_organizations_unit()
    {
        var ownLease = await SeedLeaseAsync();
        var foreignLease = await SeedLeaseAsync();

        await using var context = CreateContext(ownLease.OrganizationId);
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var exception = await Record.ExceptionAsync(
            () => context.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE \"Leases\" SET \"UnitId\" = {foreignLease.UnitId} WHERE \"Id\" = {ownLease.LeaseId}"
            )
        );

        Assert.NotNull(exception);
        Assert.Equal("42501", FindPostgresException(exception)?.SqlState);
    }

    [Fact]
    public async Task Organization_should_not_delete_another_organizations_lease()
    {
        var foreignLease = await SeedLeaseAsync();

        await using var context = CreateContext(Guid.NewGuid());
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var affectedRows = await context.Set<Lease>()
            .Where(x => x.Id == foreignLease.LeaseId)
            .ExecuteDeleteAsync();

        Assert.Equal(0, affectedRows);
    }

    private async Task<(
        Guid OrganizationId,
        Guid PropertyId,
        Guid UnitId,
        Guid LeaseId
    )> SeedLeaseAsync()
    {
        var organization = Organization.Register(
            OrganizationName.Create($"Lease RLS Org {Guid.NewGuid():N}")
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

        await using var context = CreateContext(organization.Id);
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        await context.Set<Organization>().AddAsync(organization);
        await context.Set<Property>().AddAsync(property);
        await context.Set<Unit>().AddAsync(unit);
        await context.Set<Lease>().AddAsync(lease);

        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        return (
            organization.Id,
            property.Id,
            unit.Id,
            lease.Id
        );
    }

    private static PostgresException? FindPostgresException(
        Exception exception)
    {
        for (
            Exception? current = exception;
            current is not null;
            current = current.InnerException
        )
        {
            if (current is PostgresException postgresException)
            {
                return postgresException;
            }
        }

        return null;
    }
}
