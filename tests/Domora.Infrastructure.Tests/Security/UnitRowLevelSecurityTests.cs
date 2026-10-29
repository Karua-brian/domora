
using Domora.Application.Common.Context;
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

public sealed class UnitRowLevelSecurityTests
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
    public async Task Organization_should_read_its_own_unit()
    {
        var seeded = await SeedUnitAsync();

        await using var context = CreateContext(seeded.OrganizationId);
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var result = await context.Set<Unit>()
            .SingleOrDefaultAsync(x => x.Id == seeded.UnitId);

        Assert.NotNull(result);
        Assert.Equal(seeded.PropertyId, result.PropertyId);
    }

    [Fact]
    public async Task Organization_should_not_read_another_organizations_unit()
    {
        var seeded = await SeedUnitAsync();

        await using var context = CreateContext(Guid.NewGuid());
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var result = await context.Set<Unit>()
            .SingleOrDefaultAsync(x => x.Id == seeded.UnitId);

        Assert.Null(result);
    }

    [Fact]
    public async Task Organization_should_not_insert_unit_for_another_organizations_property()
    {
        var foreignProperty = await SeedUnitAsync();

        var invalidUnit = Unit.Register(
            foreignProperty.PropertyId,
            UnitNumber.Create($"U-{Guid.NewGuid():N}"),
            UnitType.Apartment
        );

        await using var context = CreateContext(Guid.NewGuid());
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        await context.Set<Unit>().AddAsync(invalidUnit);

        var exception = await Record.ExceptionAsync(
            () => context.SaveChangesAsync()
        );

        Assert.NotNull(exception);
        Assert.Equal("42501", FindPostgresException(exception)?.SqlState);
    }

    [Fact]
    public async Task Organization_should_not_reassign_its_unit_to_another_organizations_property()
    {
        var ownUnit = await SeedUnitAsync();
        var foreignUnit = await SeedUnitAsync();

        await using var context = CreateContext(ownUnit.OrganizationId);
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var exception = await Record.ExceptionAsync(
            () => context.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE \"Units\" SET \"PropertyId\" = {foreignUnit.PropertyId} WHERE \"Id\" = {ownUnit.UnitId}"
            )
        );

        Assert.NotNull(exception);
        Assert.Equal("42501", FindPostgresException(exception)?.SqlState);
    }

    [Fact]
    public async Task Organization_should_not_delete_another_organizations_unit()
    {
        var foreignUnit = await SeedUnitAsync();

        await using var context = CreateContext(Guid.NewGuid());
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var affectedRows = await context.Set<Unit>()
            .Where(x => x.Id == foreignUnit.UnitId)
            .ExecuteDeleteAsync();

        Assert.Equal(0, affectedRows);
    }

    private async Task<(
        Guid OrganizationId,
        Guid PropertyId,
        Guid UnitId
    )> SeedUnitAsync()
    {
        var organization = Organization.Register(
            OrganizationName.Create($"Unit RLS Org {Guid.NewGuid():N}")
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

        await using var context = CreateContext(organization.Id);
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        await context.Set<Organization>().AddAsync(organization);
        await context.Set<Property>().AddAsync(property);
        await context.Set<Unit>().AddAsync(unit);

        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        return (organization.Id, property.Id, unit.Id);
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
