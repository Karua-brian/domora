namespace Domora.Domain.Units;

public interface IUnitRepository
{
    Task AddAsync(Unit unit, CancellationToken cancellationToken = default);
}