using Domora.Application.Common.Exceptions;
using Domora.Domain.Common.Exceptions;
using Domora.Application.Common.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Domora.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly DomoraDbContext _dbContext;

    public UnitOfWork(
        DomoraDbContext dbContext
    )
    {
        _dbContext = dbContext;
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
}