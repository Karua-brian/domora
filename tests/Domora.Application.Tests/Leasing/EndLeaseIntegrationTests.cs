using Domora.Application.Common.Context;
using Domora.Application.Common.Exceptions;
using Domora.Application.Leasing.Commands.EndLease;
using Domora.Domain.Common;
using Domora.Domain.Common.Exceptions;
using Domora.Domain.Leasing;
using Domora.Domain.Leasing.Enums;
using Domora.Domain.Organizations;
using Domora.Domain.Organizations.ValueObjects;
using Domora.Domain.Properties;
using Domora.Domain.Properties.ValueObjects;
using Domora.Domain.Units;
using Domora.Domain.Units.Enums;
using Domora.Domain.Units.ValueObjects;
using Domora.Infrastructure.Persistence;
using Domora.Infrastructure.Persistence.Interceptors;
using Domora.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Domora.Application.Tests.Leasing;

public sealed class EndLeaseIntegrationTests
{
    private readonly DbContextOptions<DomoraDbContext> _options;

    private readonly string _connectionString;

    public EndLeaseIntegrationTests()
    {
       _connectionString = Environment.GetEnvironmentVariable("DomoraTest")
        ??  throw new InvalidOperationException(
                "DomoraTest connection string is not configured."
            );

        _options = new DbContextOptionsBuilder<DomoraDbContext>{}
            .UseNpgsql(_connectionString)
            .Options;
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

    private sealed class TestOrganizationContext : IOrganizationContext
    {
        public TestOrganizationContext(Guid organizationId)
        {
            OrganizationId = organizationId;
        }

        public Guid OrganizationId { get; }
    }

    private async Task<(Guid UnitId, Guid LeaseId)> CreateTestActiveLeaseAsync()
    {
        await using var context = new DomoraDbContext(_options);

        var organization = Organization.Register(
            OrganizationName.Create($"EndLease Org {Guid.NewGuid():N}")
        );

        var property = Property.Register(
            organization.Id,
            PropertyName.Create($"EndLease Prop {Guid.NewGuid():N}")
        ); 

        var unit = Unit.Register(
            property.Id,
            UnitNumber.Create($"ENDLEASE-{Guid.NewGuid():N}"),
            UnitType.Bedsitter
        );
        unit.Occupy();

        var lease = Lease.Register(
            unit.Id,
            Guid.NewGuid(),
            new Money(15000m, "KES")
        );

        await ExecuteAsOrganizationContextAsync(
            organization.Id,
            async ctx =>
            {
                await ctx.Organizations.AddAsync(organization);
                await ctx.Properties.AddAsync(property);
                await ctx.Units.AddAsync(unit);
                await ctx.Leases.AddAsync(lease);
                await ctx.SaveChangesAsync();
            }
        );

        return (unit.Id, lease.Id);
    }

    private sealed class AsyncCoordinatedLeaseRepository : ILeaseRepository
    {
        private readonly ILeaseRepository _inner;
        private readonly AsyncLoadBarrier _barrier;

        public AsyncCoordinatedLeaseRepository(
            ILeaseRepository inner,
            AsyncLoadBarrier barrier
        )
        {
            _inner = inner;
            _barrier = barrier;
        }

        public async Task<Lease?> GetByIdAsync(
            Guid id, 
            CancellationToken cancellationToken = default
        )
        {
            var lease = await _inner.GetByIdAsync(
                id, 
                cancellationToken
            );

            await _barrier.SignalAndWaitAsync(cancellationToken);

            return lease;
        }

        public Task AddAsync(
            Lease lease, 
            CancellationToken token) 
            => _inner.AddAsync(lease, token);
        public Task UpdateAsync(
            Lease lease, 
            CancellationToken token) 
            => _inner.UpdateAsync(lease, token);
    }

    private sealed class AsyncCoordinatedUnitRepository : IUnitRepository
    {
        private readonly IUnitRepository _inner;
        private readonly AsyncLoadBarrier _barrier;

        public AsyncCoordinatedUnitRepository(
            IUnitRepository inner,
            AsyncLoadBarrier barrier
        )
        {
            _inner = inner;
            _barrier = barrier;
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

            await _barrier.SignalAndWaitAsync(
                cancellationToken
            );

            return unit;
        }

        public Task AddAsync(
            Unit unit,
            CancellationToken token
        )
            => _inner.AddAsync(unit, token);

        public Task UpdateAsync(
            Unit unit,
            CancellationToken token
        )
            => _inner.UpdateAsync(unit, token);
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
    public async Task Created_active_lease_should_have_occupied_unit()
    {
        var (unitId, _) =await CreateTestActiveLeaseAsync();

        await using var context =
            new DomoraDbContext(_options);

        var unit = await context.Units
            .SingleAsync(u => u.Id == unitId);

        Assert.Equal(
            OccupancyStatus.Occupied,
            unit.Status
        );
    }

    [Fact]
    public async Task Ending_active_lease_should_end_lease_and_vacate_unit()
    {
        // Arrange 
        var (unitId, leaseId) = await CreateTestActiveLeaseAsync();

        await using var context = new DomoraDbContext(_options);

        var organizationContext = new TestOrganizationContext(Guid.NewGuid());

        var handler = new EndLeaseHandler(
            new LeaseRepository(context),
            new UnitRepository(context),
            new UnitOfWork(context, organizationContext)
        );

        var command = new EndLeaseCommand(
            leaseId,
            DateOnly.FromDateTime(DateTime.UtcNow)
        );

        // Act
        var response = await handler.Handle(
            command,
            CancellationToken.None
        );

        // Assert
        Assert.Equal(LeaseStatus.Ended, response.Status);

        await using var verificationContext = new DomoraDbContext(_options);

        var persistedLease = await verificationContext.Leases
            .SingleAsync(l => l.Id == leaseId);

        var persistedUnit = await verificationContext.Units
            .SingleAsync(u => u.Id == unitId);

        Assert.Equal(
            LeaseStatus.Ended,
            persistedLease.Status
        );

        Assert.Equal(
            OccupancyStatus.Vacant,
            persistedUnit.Status
        );
    }

    [Fact]
    public async Task Ending_already_ended_lease_should_fail()
    {
        // Arrange
        var (_, leaseId) = await CreateTestActiveLeaseAsync();

        await using var contextSetup = new DomoraDbContext(_options);

        var preEndedLease = await contextSetup.Leases
            .SingleAsync(l => l.Id == leaseId);

        var unit = await contextSetup.Units
            .SingleAsync(u => u.Id == preEndedLease.UnitId);

        preEndedLease.EndLease(DateOnly.FromDateTime(DateTime.UtcNow));
        unit.Vacate();

        await contextSetup.SaveChangesAsync();

        await using var contextExecution = new DomoraDbContext(_options);

        var organizationContext = new TestOrganizationContext(Guid.NewGuid());

        var handler = new EndLeaseHandler(
            new LeaseRepository(contextExecution),
            new UnitRepository(contextExecution),
            new UnitOfWork(contextExecution, organizationContext)
        );

        var command = new EndLeaseCommand(
            leaseId,
            DateOnly.FromDateTime(DateTime.UtcNow)
        );

        // Act & Assert
        await Assert.ThrowsAsync<ResourceConflictException>(async () =>
        {
            await handler.Handle(
                command,
                CancellationToken.None
            );
        });
    }

    [Fact]
    public async Task Ending_lease_before_start_date_should_fail()
    {
        // Arrange
        var (_, leaseId) = await CreateTestActiveLeaseAsync();

        await using var context = new DomoraDbContext(_options);

        var organizationContext = new TestOrganizationContext(Guid.NewGuid());

        var handler = new EndLeaseHandler(
            new LeaseRepository(context),
            new UnitRepository(context),
            new UnitOfWork(context, organizationContext)
        );

        var invalidBeforeDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-5);

        var command = new EndLeaseCommand(
            leaseId,
            invalidBeforeDate
        );

        // Act & Assert
        await Assert.ThrowsAsync<DomainValidationException>(async () =>
        {
            await handler.Handle(
                command,
                CancellationToken.None
            );
        });
    }

    [Fact]
    public async Task Concurrent_lease_termination_requests_should_only_process_once()
    {
        // 1. Arrange
        var (_, leaseId) = await CreateTestActiveLeaseAsync();

        await using var contextA = new DomoraDbContext(_options);
        await using var contextB = new DomoraDbContext(_options);

        // Clear trackers to ensure a clean slate
        contextA.ChangeTracker.Clear(); 
        contextB.ChangeTracker.Clear();

        var leaseBarrier = new AsyncLoadBarrier(2);
        var unitBarrier = new AsyncLoadBarrier(2);
        
        // Wrap your real repositories with the async decorator
        var coordinatedRepoA = 
            new AsyncCoordinatedLeaseRepository(
            new LeaseRepository(contextA), 
            leaseBarrier
        );

        var coordinatedRepoB = 
            new AsyncCoordinatedLeaseRepository(
            new LeaseRepository(contextB), 
            leaseBarrier
        );

        var coordinatedUnitRepoA =
            new AsyncCoordinatedUnitRepository(
            new UnitRepository(contextA),
            unitBarrier
        );

        var coordinatedUnitRepoB =
            new AsyncCoordinatedUnitRepository(
            new UnitRepository(contextB),
            unitBarrier
        );

        var organizationContext = new TestOrganizationContext(Guid.NewGuid());

        var handlerA = new EndLeaseHandler(
            coordinatedRepoA,
            coordinatedUnitRepoA,
            new UnitOfWork(contextA, organizationContext)
        );

        var handlerB = new EndLeaseHandler(
            coordinatedRepoB,
            coordinatedUnitRepoB,
            new UnitOfWork(contextB, organizationContext)
        );

        var commandA = new EndLeaseCommand(leaseId, DateOnly.FromDateTime(DateTime.UtcNow));
        var commandB = new EndLeaseCommand(leaseId, DateOnly.FromDateTime(DateTime.UtcNow));

        // 2. Act
        var taskA = CaptureAsync(handlerA.Handle(commandA, CancellationToken.None));
        var taskB = CaptureAsync(handlerB.Handle(commandB, CancellationToken.None));

        var results = await Task.WhenAll(taskA, taskB);

        // 3. Assert
        var successfulOperations = results.Count(x => x.Exception is null);
    
        // Your UnitOfWork wraps DbUpdateConcurrencyException into ConcurrencyException!
        var conflictFailures = results.Count(x => x.Exception is ConcurrencyException);

        // Verify exactly one wins the commit, and exactly one catches your custom ConcurrencyException wrapper
        Assert.Equal(1, successfulOperations);
        Assert.Equal(1, conflictFailures);

        await using var verificationContext = 
            new DomoraDbContext(_options);

        var persistedLease = await verificationContext.Leases
            .SingleAsync(l => l.Id == leaseId);

        var persistedUnit = await verificationContext.Units
            .SingleAsync(u => u.Id == persistedLease.UnitId);

        Assert.Equal(
            LeaseStatus.Ended,
            persistedLease.Status
        );

        Assert.Equal(
            OccupancyStatus.Vacant,
            persistedUnit.Status
        );

    }

    private sealed class AsyncLoadBarrier
    {
        private readonly int _expected;
        private int _arrived;

        private readonly TaskCompletionSource _tcs =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public AsyncLoadBarrier(int expected)
        {
            _expected = expected;
        }

        public async Task SignalAndWaitAsync(
            CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref _arrived) == _expected)
            {
                _tcs.TrySetResult();
            }

            await _tcs.Task.WaitAsync(cancellationToken);
        }
    }

}