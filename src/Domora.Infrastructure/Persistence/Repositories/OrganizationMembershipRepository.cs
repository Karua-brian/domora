using Domora.Application.Common.Persistence;
using Domora.Domain.Organizations;
using Domora.Domain.Organizations.Enums;
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

    public async Task AddAsync(
        OrganizationMembership membership,
        CancellationToken cancellationToken
    )
    {
        await _dbContext.OrganizationMemberships
            .AddAsync(
                membership,
                cancellationToken
        );
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

    public Task<bool> HasRoleAsync(
        Guid userId,
        Guid organizationId,
        OrganizationRole role,
        CancellationToken cancellationToken
    )
    {
        return _dbContext.OrganizationMemberships
            .AsNoTracking()
            .AnyAsync(
                membership => 
                    membership.UserId == userId &&
                    membership.OrganizationId == organizationId &&
                    membership.Role == role,
                cancellationToken
            );
    }
}