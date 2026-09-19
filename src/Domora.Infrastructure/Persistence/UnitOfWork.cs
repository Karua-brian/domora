using Domora.Application.Common.Exceptions;
using Domora.Domain.Common.Exceptions;
using Domora.Application.Common.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Microsoft.EntityFrameworkCore.Storage;
using Domora.Application.Common.Context;

namespace Domora.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly DomoraDbContext _dbContext;

    private readonly IOrganizationContext _organizationContext;

    public UnitOfWork(
        DomoraDbContext dbContext,
        IOrganizationContext organizationContext
    )
    {
        _dbContext = dbContext;
        _organizationContext = organizationContext;
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken
    )
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {

            throw new ConcurrencyException(
                "The resource was modified by another operation.",
                ex
                );
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException postgresException 
                && postgresException.SqlState == PostgresErrorCodes.UniqueViolation
                && postgresException.ConstraintName == "IX_Leases_UnitId" 
                )
            {

                throw new ResourceConflictException(
                    "The unit already has an active lease.",
                    ex
                );    
            }
    }   

    public async Task<ITransaction> BeginTransactionAsync(
        CancellationToken cancellationToken
    )
    {
        var organizationId = _organizationContext.OrganizationId;

        if (organizationId == Guid.Empty)
            throw new InvalidOperationException(
                "Organization context is unavailable"
            );

        var transaction = await _dbContext.Database
            .BeginTransactionAsync(cancellationToken);

        return new EfCoreTransaction(transaction);
    }
}

public sealed class EfCoreTransaction(
    IDbContextTransaction transaction
    ) : ITransaction
{
    private readonly IDbContextTransaction _transaction = transaction;

    public async Task CommitAsync(
        CancellationToken cancellationToken
    )
    {
        await _transaction.CommitAsync(cancellationToken);
    }

    public async Task RollbackAsync(
        CancellationToken cancellationToken
    )
    {
        await _transaction.RollbackAsync(cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return _transaction.DisposeAsync();
    }
}