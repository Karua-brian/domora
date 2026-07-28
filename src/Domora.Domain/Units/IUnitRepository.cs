namespace Domora.Domain.Units;

public interface IUnitRepository
{
    Task AddAsync(Unit unit, CancellationToken cancellationToken = default);

    Task<Unit?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task UpdateAsync(Unit unit, CancellationToken cancellationToken = default);
}