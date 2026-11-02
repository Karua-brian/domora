using Domora.Domain.Organizations.Enums;

namespace Domora.Application.Organizations.Commands.ChangeOrganizationMemberRole;

public sealed record ChangeOrganizationMemberRoleCommand(Guid UserId, OrganizationRole Role);
