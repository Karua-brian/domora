namespace Domora.Domain.Leasing;

public interface ILeaseRepository
{

    Task AddAsync(
        Lease lease, 
        CancellationToken cancellationToken = default
    );

    Task<Lease?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    );

}
