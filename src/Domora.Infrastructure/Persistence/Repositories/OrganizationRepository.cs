using Domora.Domain.Organizations;

namespace Domora.Infrastructure.Persistence.Repositories;

public sealed class OrganizationRepository : IOrganizationRepository
{
    private readonly DomoraDbContext _dbContext;

    public OrganizationRepository(DomoraDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public async Task AddAsync(Organization organization, CancellationToken cancellationToken = default)
    {
        await _dbContext.Organizations.AddAsync(organization, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}