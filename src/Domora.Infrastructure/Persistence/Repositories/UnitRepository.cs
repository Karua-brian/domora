using Domora.Domain.Units;

namespace Domora.Infrastructure.Persistence.Repositories;

public sealed class UnitRepository : IUnitRepository
{
    private readonly DomoraDbContext _dbContext;

    public UnitRepository(DomoraDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Unit unit, CancellationToken cancellationToken)
    {
        await _dbContext.Units.AddAsync(unit, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}