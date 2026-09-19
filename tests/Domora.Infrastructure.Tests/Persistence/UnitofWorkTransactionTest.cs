using Domora.Application.Common.Context;
using Domora.Domain.Organizations;
using Domora.Domain.Organizations.ValueObjects;
using Domora.Infrastructure.Persistence;
using Domora.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace Domora.Infrastructure.Tests.Persistence;

public sealed class UnitofWorkTransactionTests
{
    private readonly DbContextOptions<DomoraDbContext> _options;

    public UnitofWorkTransactionTests()
    {
        var connectionString = 
            Environment.GetEnvironmentVariable("DomoraTest");

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException( 
                 "DomoraTest connection string is not configured."
            );

        _options = new DbContextOptionsBuilder<DomoraDbContext>()
            .UseNpgsql(connectionString)
            .Options;
    }

    private sealed class TestOrganizationContext : IOrganizationContext
    {
        public TestOrganizationContext(Guid organizationId)
        {
            OrganizationId = organizationId;
        }

        public Guid OrganizationId { get; }

    }
    
    [Fact]
    public async Task Transaction_should_commit_changes()
    {
        await using var context = new DomoraDbContext(_options);

        var organizationContext = new TestOrganizationContext(Guid.NewGuid());

        var unitOfWork = new UnitOfWork(
            context,
            organizationContext
        );

        var organization = Organization.Register(
            OrganizationName.Create(
                $"Transaction Commit {Guid.NewGuid():N}"
            )
        );

        await context.Organizations.AddAsync(organization);

        await using var transaction = await unitOfWork.BeginTransactionAsync(CancellationToken.None);

        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        await transaction.CommitAsync();

        await using var verificationContext = new DomoraDbContext(_options);

        var persisted = await verificationContext.Organizations
            .SingleOrDefaultAsync(
                organizationRecord => 
                organizationRecord.Id == organization.Id
            );

        Assert.NotNull(persisted);
    }

    [Fact]
    public async Task Transaction_should_rollback_changes()
    {
        await using var context = new DomoraDbContext(_options);

        var organizationContext = new TestOrganizationContext(Guid.NewGuid());

        var unitOfWork = new UnitOfWork(
            context,
            organizationContext
        );

        var organization = Organization.Register(
            OrganizationName.Create($"Transaction Rollback {Guid.NewGuid():N}")
        );

        await context.Organizations.AddAsync(organization);

        await using var transaction = await unitOfWork.BeginTransactionAsync(CancellationToken.None);

        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        await transaction.RollbackAsync();

        await using var verificationContext = new DomoraDbContext(_options);

        var perisited = await verificationContext.Organizations
            .SingleOrDefaultAsync(
                organizationRecord => 
                    organizationRecord.Id == organization.Id
            );

        Assert.Null(perisited);
    }

    [Fact]
    public async Task Beginning_transaction_without_organization_context_should_fail()
    {
        await using var context = new DomoraDbContext(_options);

        var organizationContext = new TestOrganizationContext(Guid.Empty);

        var unitOfWork = new UnitOfWork(
            context,
            organizationContext
        );

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await unitOfWork.BeginTransactionAsync(CancellationToken.None)
        );
    }

    [Fact]
    public async Task Beginning_transaction_with_organization_context_should_succeed()
    {
        await using var context = new DomoraDbContext(_options);

        var organizationContext = new TestOrganizationContext(Guid.NewGuid());

        var unitOfWork = new UnitOfWork(
            context,
            organizationContext
        );

        await using var transaction = await unitOfWork.BeginTransactionAsync(CancellationToken.None);

        Assert.NotNull(transaction);

        await transaction.RollbackAsync();

    }

}