namespace Domora.Application.Common.Authorization;

public interface IOrganizationAccess
{
    Task<bool> CanAccessAsync(
        Guid userId,
        Guid organizationId,
        CancellationToken cancellationToken = default
    );
}