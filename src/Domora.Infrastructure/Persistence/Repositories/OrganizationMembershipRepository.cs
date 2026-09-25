using Domora.Application.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Domora.Infrastructure.Persistence.Repositories;

public sealed class OrganizationMembershipRepository : IOrganizationMembershipRepository
{
    private readonly DomoraDbContext _dbContext;

    public OrganizationMembershipRepository(
        DomoraDbContext dbContext
    )
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsAsync(
        Guid userId,
        Guid organizationId,
        CancellationToken cancellationToken
    )
    {
        return _dbContext.OrganizationMemberships
            .AsNoTracking()
            .AnyAsync(
                membership => 
                    membership.UserId == userId &&
                    membership.OrganizationId == organizationId,
                cancellationToken
            );
    }
}