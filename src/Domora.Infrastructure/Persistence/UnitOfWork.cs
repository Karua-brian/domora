using Domora.Application.Common.Persistence;

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
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}