using Domora.Application.Common.Persistence;

namespace Domora.Application.Common.Authorization;

public sealed class OrganizationAccess : IOrganizationAccess
{
    private readonly IOrganizationMembershipRepository _memberships;

    public OrganizationAccess(
        IOrganizationMembershipRepository memberships
    )
    {
        _memberships = memberships;
    }

    public Task<bool> CanAccessAsync(
        Guid userId,
        Guid organizationId,
        CancellationToken cancellationToken
    )
    {
        return _memberships.ExistsAsync(
            userId,
            organizationId,
            cancellationToken
        );
    }
}