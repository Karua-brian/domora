namespace Domora.Domain.Leasing;

public interface ILeaseRepository
{

    Task AddAsync(Lease lease, CancellationToken cancellationToken = default);

    // void Update(Lease lease);

    // void Remove(Lease lease);

    // Lease? GetById(Guid id);

}
