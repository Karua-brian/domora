namespace Domora.Domain.Leasing;

public interface ILeaseRepository
{
    Lease? GetById(Guid id);

    void Add(Lease lease);

    void Update(Lease lease);

    void Remove(Lease lease);
}
