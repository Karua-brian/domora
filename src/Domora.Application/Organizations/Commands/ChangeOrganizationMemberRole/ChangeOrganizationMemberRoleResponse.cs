using Domora.Domain.Organizations.Enums;

namespace Domora.Application.Organizations.Commands.ChangeOrganizationMemberRole;

public sealed record ChangeOrganizationMemberRoleResponse(
    Guid OrganizationId,
    Guid UserId,
    OrganizationRole Role);
