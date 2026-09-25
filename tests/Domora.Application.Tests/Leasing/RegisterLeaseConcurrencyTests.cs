using Domora.Application.Common.Context;
using Domora.Application.Leasing.Commands.RegisterLease;
using Domora.Domain.Common;
using Domora.Domain.Common.Exceptions;
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

public sealed class RegisterLeaseConcurrencyTests
{
    private readonly DbContextOptions<DomoraDbContext> _options;
    private readonly string _connectionString;

    public RegisterLeaseConcurrencyTests()
    {
        _connectionString =
            Environment.GetEnvironmentVariable("DomoraTest")
            ?? throw new InvalidOperationException(
                "DomoraTest connection string is not configured.");

        _options = new DbContextOptionsBuilder<DomoraDbContext>()
            .UseNpgsql(_connectionString)
            .Options;
    }

    // ============================================================
    // Organization context
    // ============================================================

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
        Func<DomoraDbContext, Task> action)
    {
        var organizationContext =
            new TestOrganizationContext(organizationId);

        var interceptor =
            new OrganizationTransactionInterceptor(
                organizationContext);

        var options =
            new DbContextOptionsBuilder<DomoraDbContext>()
                .UseNpgsql(_connectionString)
                .AddInterceptors(interceptor)
                .Options;

        await using var context =
            new DomoraDbContext(options);

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        await action(context);

        await transaction.CommitAsync();
    }

    // ============================================================
    // Test data
    // ============================================================

    private async Task<(Guid UnitId, Guid OrganizationId)> CreateTestUnitAsync()
    {
        var organization = Organization.Register(
            OrganizationName.Create(
                $"Concurrency Organization {Guid.NewGuid():N}")
        );

        var property = Property.Register(
            organization.Id,
            PropertyName.Create(
                $"Concurrency Property {Guid.NewGuid():N}")
        );

        var unit = Unit.Register(
            property.Id,
            UnitNumber.Create(
                $"CONCURRENCY-{Guid.NewGuid():N}"),
            UnitType.Bedsitter
        );

        await ExecuteAsOrganizationContextAsync(
            organization.Id,
            async context =>
            {
                await context.Organizations.AddAsync(
                    organization);

                await context.Properties.AddAsync(
                    property);

                await context.Units.AddAsync(
                    unit);

                await context.SaveChangesAsync();
            });

        return (unit.Id, organization.Id);
    }

    // ============================================================
    // Concurrency coordination
    // ============================================================

    private sealed class CoordinatedUnitRepository
        : IUnitRepository
    {
        private readonly IUnitRepository _inner;
        private readonly Barrier _barrier;

        public CoordinatedUnitRepository(
            IUnitRepository inner,
            Barrier barrier)
        {
            _inner = inner;
            _barrier = barrier;
        }

        public Task AddAsync(
            Unit unit,
            CancellationToken cancellationToken = default)
        {
            return _inner.AddAsync(
                unit,
                cancellationToken);
        }

        public async Task<Unit?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var unit = await _inner.GetByIdAsync(
                id,
                cancellationToken);

            // Both handlers must reach this point before either
            // continues. This creates the intended race.
            _barrier.SignalAndWait(
                cancellationToken);

            return unit;
        }

        public Task UpdateAsync(
            Unit unit,
            CancellationToken cancellationToken = default)
        {
            return _inner.UpdateAsync(
                unit,
                cancellationToken);
        }
    }

    // ============================================================
    // Operation result helper
    // ============================================================

    private sealed record OperationResult<T>(
        T? Value,
        Exception? Exception);

    private static async Task<OperationResult<T>> CaptureAsync<T>(
        Task<T> operation)
    {
        try
        {
            var result = await operation;

            return new OperationResult<T>(
                result,
                null);
        }
        catch (Exception exception)
        {
            return new OperationResult<T>(
                default,
                exception);
        }
    }

    // ============================================================
    // Tests
    // ============================================================

    [Fact]
    public async Task Concurrent_registration_for_same_unit_should_allow_only_one_active_lease()
    {
        // --------------------------------------------------------
        // Arrange
        // --------------------------------------------------------

        var (unitId, organizationId) =
            await CreateTestUnitAsync();

        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        await using var contextA =
            new DomoraDbContext(_options);

        await using var contextB =
            new DomoraDbContext(_options);

        var barrier = new Barrier(2);

        var unitRepositoryA =
            new CoordinatedUnitRepository(
                new UnitRepository(contextA),
                barrier);

        var unitRepositoryB =
            new CoordinatedUnitRepository(
                new UnitRepository(contextB),
                barrier);

        var organizationContext = new TestOrganizationContext(organizationId);

        var handlerA =
            new RegisterLeaseHandler(
                new LeaseRepository(contextA),
                unitRepositoryA,
                new UnitOfWork(
                    contextA,
                    organizationContext));

        var handlerB =
            new RegisterLeaseHandler(
                new LeaseRepository(contextB),
                unitRepositoryB,
                new UnitOfWork(
                    contextB,
                    organizationContext));

        var commandA =
            new RegisterLeaseCommand(
                unitId,
                tenantA,
                new Money(15000m, "KES"));

        var commandB =
            new RegisterLeaseCommand(
                unitId,
                tenantB,
                new Money(15000m, "KES"));

        // --------------------------------------------------------
        // Act
        // --------------------------------------------------------

        var taskA =
            CaptureAsync(
                handlerA.Handle(
                    commandA,
                    CancellationToken.None));

        var taskB =
            CaptureAsync(
                handlerB.Handle(
                    commandB,
                    CancellationToken.None));

        var results =
            await Task.WhenAll(
                taskA,
                taskB);

        // --------------------------------------------------------
        // Assert: exactly one registration succeeds
        // --------------------------------------------------------

        var successfulOperations =
            results.Count(
                result => result.Exception is null);

        var conflictFailures =
            results.Count(
                result =>
                    result.Exception
                    is ResourceConflictException);

        Assert.Equal(
            1,
            successfulOperations);

        Assert.Equal(
            1,
            conflictFailures);

        // --------------------------------------------------------
        // Assert: database state
        // --------------------------------------------------------

        await using var verificationContext =
            new DomoraDbContext(_options);

        var activeLeases =
            await verificationContext.Leases
                .Where(lease =>
                    lease.UnitId == unitId &&
                    lease.Status == LeaseStatus.Active)
                .ToListAsync();

        Assert.Single(activeLeases);

        var persistedUnit =
            await verificationContext.Units
                .SingleAsync(unit =>
                    unit.Id == unitId);

        Assert.Equal(
            OccupancyStatus.Occupied,
            persistedUnit.Status);
    }
}