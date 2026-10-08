using Domora.Domain.Organizations;
using Domora.Domain.Organizations.Enums;

namespace Domora.Application.Common.Persistence;

public interface IOrganizationMembershipRepository
{
    Task AddAsync(
        OrganizationMembership membership,
        CancellationToken cancellationToken = default
    );

    Task<bool> ExistsAsync(
        Guid userId,
        Guid organizationId,
        CancellationToken cancellationToken = default
    );

    Task<bool> HasRoleAsync(
        Guid userId,
        Guid organizationId,
        OrganizationRole role,
        CancellationToken cancellationToken = default
    ); 
}