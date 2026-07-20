namespace Domora.Domain.Organizations;

public interface IOrganizationRepository
{
    Task AddAsync(Organization organization, CancellationToken cancellationToken = default);

    // Task <Organization?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // Task UpdateAsync(Organization organization, CancellationToken cancellationToken = default);

    // Task DeleteAsync(Organization organization, CancellationToken cancellationToken = default);
}