namespace Domora.Application.Common.Persistence;

public interface IOrganizationMembershipRepository
{
    Task<bool> ExistsAsync(
        Guid userId,
        Guid organizationId,
        CancellationToken cancellationToken = default
    );
}