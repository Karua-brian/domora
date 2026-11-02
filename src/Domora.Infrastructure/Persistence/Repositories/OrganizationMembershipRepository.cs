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

    public Task RemoveAsync(
        OrganizationMembership membership,
        CancellationToken cancellationToken
    )
    {
        _dbContext.OrganizationMemberships.Remove(membership);
        return Task.CompletedTask;
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

    public async Task LockOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken
    )
    {
        var organizationIdResult = await _dbContext.Database
            .SqlQuery<Guid>($"""
                SELECT "Id" AS "Value"
                FROM "Organizations"
                WHERE "Id" = {organizationId}
                FOR UPDATE
                """)
            .SingleOrDefaultAsync(cancellationToken);

        if (organizationIdResult == Guid.Empty)
            throw new Domora.Application.Common.Exceptions.NotFoundException(
                "Organization not found."
            );
    }

    public async Task<IReadOnlyList<OrganizationMembership>> GetOwnersAsync(
        Guid organizationId,
        CancellationToken cancellationToken
    )
    {
        return await _dbContext.OrganizationMemberships
            .Where(
                membership => membership.OrganizationId == organizationId &&
                    membership.Role == OrganizationRole.Owner)
            .ToListAsync(cancellationToken);
    }

    public Task<OrganizationMembership?> GetByUserAsync(
        Guid userId,
        Guid organizationId,
        CancellationToken cancellationToken
    )
    {
        return _dbContext.OrganizationMemberships
            .SingleOrDefaultAsync(
                membership => membership.UserId == userId &&
                    membership.OrganizationId == organizationId,
                cancellationToken
            );
    }

}
