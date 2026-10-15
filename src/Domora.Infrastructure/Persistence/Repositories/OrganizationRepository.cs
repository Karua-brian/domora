using Domora.Domain.Organizations;
using Microsoft.EntityFrameworkCore;

namespace Domora.Infrastructure.Persistence.Repositories;

public sealed class OrganizationRepository : IOrganizationRepository
{
    private readonly DomoraDbContext _dbContext;

    public OrganizationRepository(DomoraDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public async Task AddAsync(
        Organization organization, 
        CancellationToken cancellationToken = default
    )
    {
        await _dbContext.Organizations.AddAsync(
            organization, 
            cancellationToken
        );

    }

    public async Task<IReadOnlyCollection<Guid>> GetAllIdsAsync(
        CancellationToken cancellationToken
    )
    {
        return await _dbContext.Organizations
            .AsNoTracking()
            .Select(o => o.Id)
            .ToListAsync(cancellationToken);
    }
}