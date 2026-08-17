using Domora.Application.Common.Exceptions;
using Domora.Application.Common.Persistence;
using Microsoft.EntityFrameworkCore;

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
    }   
}