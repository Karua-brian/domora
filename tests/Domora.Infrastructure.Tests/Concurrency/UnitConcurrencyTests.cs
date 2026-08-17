using Domora.Domain.Organizations;
using Domora.Domain.Organizations.ValueObjects;
using Domora.Domain.Properties;
using Domora.Domain.Properties.ValueObjects;
using Domora.Domain.Units;
using Domora.Domain.Units.Enums;
using Domora.Domain.Units.ValueObjects;
using Domora.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Domora.Infrastructure.Tests.Concurrency;

public sealed class UnitConcurrencyTests
{
    private readonly DbContextOptions<DomoraDbContext> _options;

    public UnitConcurrencyTests()
    {
        var connectionString = Environment.GetEnvironmentVariable(
            "DomoraTest"
        );

        if (string.IsNullOrWhiteSpace(connectionString)) 
            throw new InvalidOperationException(
                "DomoraTest connection is not configured"
            );

        _options = new DbContextOptionsBuilder<DomoraDbContext>()
            .UseNpgsql(connectionString)
            .Options;
    }


    [Fact]
    public async Task Two_contexts_loading_same_unit_should_detect_stale_update()
    {
        // Arrange
        Guid unitId;

        await using (var context = new DomoraDbContext(_options))
        {
            var organization = Organization.Register(
               OrganizationName.Create($"Test organization {Guid.NewGuid():N}") 
            );

            await context.Organizations.AddAsync(organization);

            var property = Property.Register(
                organization.Id,
                PropertyName.Create($"Test Property {Guid.NewGuid():N}")
            );

            await context.Properties.AddAsync(property); 

            var unit = Unit.Register(
                property.Id,
                UnitNumber.Create($"TEST-{Guid.NewGuid():N}"),
                UnitType.OneBedroom
            );

            await context.Units.AddAsync(unit);

            await context.SaveChangesAsync();

            unitId = unit.Id;
        }

        await using var contextA = new DomoraDbContext(_options);

        await using var contextB = new DomoraDbContext(_options);

        var unitA = await contextA.Units
            .SingleAsync(u => u.Id == unitId);

        var unitB = await contextB.Units
            .SingleAsync(u => u.Id == unitId);

        // Both read the same version.
        Assert.Equal(unitA.Version, unitB.Version);

        Assert.Equal(
            OccupancyStatus.Vacant,
            unitA.Status
        );

        // Act
        unitA.Occupy();
        unitB.Occupy();

        await contextA.SaveChangesAsync();

        // Assert
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => contextB.SaveChangesAsync()
        );
    }
}
