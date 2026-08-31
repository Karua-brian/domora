using Domora.Domain.Units;
using Microsoft.EntityFrameworkCore;

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

    }

    public async Task<Unit?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Units
            .SingleOrDefaultAsync(
                unit => unit.Id == id,
                cancellationToken
            );
    }

    public async Task UpdateAsync(Unit unit, CancellationToken cancellationToken)
    {
        _dbContext.Units.Update(unit);

    }
}