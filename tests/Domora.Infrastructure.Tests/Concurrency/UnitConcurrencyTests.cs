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

namespace Domora.Infrastructure.Tests.Concurrency;

public sealed class UnitConcurrencyTests
{
    private readonly string _connectionString;

    public UnitConcurrencyTests()
    {
        _connectionString = Environment.GetEnvironmentVariable(
            "DomoraTest"
        )!;

        if (string.IsNullOrWhiteSpace(_connectionString))    
            throw new InvalidOperationException(
                "DomoraTest connection is not configured"
            );

    }

    private sealed class TestOrganizationContext : IOrganizationContext
    {
        public TestOrganizationContext(Guid organizationId)
        {
            OrganizationId = organizationId;
        }

        public Guid OrganizationId { get; }
    }

    private async Task ExecuteAsOrganizationContextAsync(
        Guid organizationId,
        Func<DomoraDbContext, Task> action
    )
    {
        var organizationContext = new TestOrganizationContext(organizationId);

        var interceptor = new OrganizationTransactionInterceptor(
            organizationContext
        );

        var options = new DbContextOptionsBuilder<DomoraDbContext>()
            .UseNpgsql(_connectionString)
            .AddInterceptors(interceptor)
            .Options;

        await using var context = new DomoraDbContext(options);

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        await action(context);

        await transaction.CommitAsync();
    }


    [Fact]
    public async Task Two_contexts_loading_same_unit_should_detect_stale_update()
    {
        // Arrange
        Guid unitId = Guid.Empty;

        var organization = Organization.Register(
            OrganizationName.Create(
                $"Test organization {Guid.NewGuid():N}"
            ) 
        );

        await ExecuteAsOrganizationContextAsync(
            organization.Id, 
            async context =>
            {
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
        );

        var organizationContext = new TestOrganizationContext(organization.Id);

        var interceptor = new OrganizationTransactionInterceptor(
            organizationContext
        );

        var options = new DbContextOptionsBuilder<DomoraDbContext>()
            .UseNpgsql(_connectionString)
            .AddInterceptors(interceptor)
            .Options;

        await using var contextA = new DomoraDbContext(options);

        await using var contextB = new DomoraDbContext(options);

        await using var transactionA =
            await contextA.Database.BeginTransactionAsync();

        await using var transactionB =
            await contextB.Database.BeginTransactionAsync();

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

        await transactionA.CommitAsync();

        // Assert
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => contextB.SaveChangesAsync()
        );

        await transactionB.RollbackAsync();
    }
}
