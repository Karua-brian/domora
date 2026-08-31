using Domora.Domain.Common.Exceptions;
using Domora.Application.Leasing.Commands.RegisterLease;
using Domora.Domain.Common;
using Domora.Domain.Leasing.Enums;
using Domora.Domain.Organizations;
using Domora.Domain.Organizations.ValueObjects;
using Domora.Domain.Properties;
using Domora.Domain.Properties.ValueObjects;
using Domora.Domain.Units;
using Domora.Domain.Units.Enums;
using Domora.Domain.Units.ValueObjects;
using Domora.Infrastructure.Persistence;
using Domora.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Domora.Application.Tests.Leasing;

public sealed class RegisterLeaseConcurrencyTests
{
    private readonly DbContextOptions<DomoraDbContext> _options;

    public RegisterLeaseConcurrencyTests()
    {
        var connectionString = Environment.GetEnvironmentVariable("DomoraTest");

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "DomoraTest connection string is not configured."
            );

        _options = new DbContextOptionsBuilder<DomoraDbContext>{}
            .UseNpgsql(connectionString)
            .Options;
    }

    private async Task<Guid> CreateTestUnitAsync()
    {
        await using var context = new DomoraDbContext(_options);

        var organization = Organization.Register(
            OrganizationName.Create($"Concurrency Organization {Guid.NewGuid():N}")
        );

        await context.Organizations.AddAsync(organization);

        var property = Property.Register(
            organization.Id,
            PropertyName.Create($"Concurrency Property {Guid.NewGuid():N}")
        );

        await context.Properties.AddAsync(property);

        var unit = Unit.Register(
            property.Id,
            UnitNumber.Create($"CONCURRENCY-{Guid.NewGuid():N}"),
            UnitType.Bedsitter
        );

        await context.Units.AddAsync(unit);

        await context.SaveChangesAsync();

        return unit.Id;
    }

    private sealed class CoordinatedUnitRepository : IUnitRepository
    {
        private readonly IUnitRepository _inner;

        private readonly Barrier _barrier;

        public CoordinatedUnitRepository(
            IUnitRepository inner,
            Barrier barrier
        )
        {
            _inner = inner;
            _barrier = barrier;
        }

        public Task AddAsync(
            Unit unit,
            CancellationToken cancellationToken = default
        ){
            return _inner.AddAsync(
                unit,
                cancellationToken
            );
        }

        public async Task<Unit?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default
        )
        {
            var unit = await _inner.GetByIdAsync(
                id,
                cancellationToken
            );

            _barrier.SignalAndWait(cancellationToken);

            return unit;
        }

        public Task UpdateAsync(
            Unit unit,
            CancellationToken cancellationToken = default
        )
        {
            return _inner.UpdateAsync(unit, cancellationToken);
        }
    }

    private static async Task<OperationResult<T>> CaptureAsync<T>(
        Task<T> operation
    )
    {
        try
        {
            var result = await operation;

            return new OperationResult<T>(
                result,
                null
            );
        }
        catch (Exception exception)
        {
            return new OperationResult<T>(
                default,
                exception
            );
        }   
    }

    private sealed record OperationResult<T>(
        T? Value,
        Exception? Exception
    );

    [Fact]
    public async Task Concurrent_registration_for_same_unit_should_allow_only_one_active_lease()
    {
        Console.WriteLine(
            $"TEST START {GetType().FullName}"
        );
        // Arrange
        var unitId = await CreateTestUnitAsync();

        var tenantA = Guid.NewGuid();

        var tenantB = Guid.NewGuid();       

        await using var contextA = new DomoraDbContext(_options);

        await using var contextB = new DomoraDbContext(_options);

        var barrier = new Barrier(2);

        var unitRepositoryA = new CoordinatedUnitRepository(
            new UnitRepository(contextA),
            barrier
        );

        var unitRepositoryB = new CoordinatedUnitRepository(
            new UnitRepository(contextB),
            barrier
        );

        var handlerA = new RegisterLeaseHandler(
            new LeaseRepository(contextA),
            unitRepositoryA,
            new UnitOfWork(contextA)
        );

        var handlerB = new RegisterLeaseHandler(
            new LeaseRepository(contextB),
            unitRepositoryB,
            new UnitOfWork(contextB)
        );

        var commandA = new RegisterLeaseCommand(
            unitId,
            tenantA,
            new Money(15000m, "KES")
        );

        var commandB = new RegisterLeaseCommand(
            unitId,
            tenantB,
            new Money(15000m, "KES")
        );

        // Act
        var taskA = CaptureAsync(
            handlerA.Handle(
            commandA,
            CancellationToken.None
            )
        );

        var taskB = CaptureAsync(
            handlerB.Handle(
            commandB,
            CancellationToken.None
           )
        );

        var results = await Task.WhenAll(
            taskA,
            taskB
        );

        var successfulOperations = results.Count(x => x.Exception is null);

        var conflictFailures = results.Count(x => x.Exception is ResourceConflictException);

        // Assert
        Assert.Equal(1, successfulOperations);

        Assert.Equal(1, conflictFailures);

        await using var verificationContext = new DomoraDbContext(_options);

        var activeLeases = await verificationContext.Leases
            .Where(l => 
                l.UnitId == unitId && 
                l.Status == LeaseStatus.Active
                )
                .ToListAsync();

        Assert.Single(activeLeases);

        var persistedUnit = await verificationContext.Units
            .SingleAsync(U => U.Id == unitId);

        Assert.Equal(OccupancyStatus.Occupied, persistedUnit.Status);   
    }
}