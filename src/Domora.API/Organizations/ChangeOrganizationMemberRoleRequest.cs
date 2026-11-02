using Domora.Domain.Organizations.Enums;

namespace Domora.API.Organizations;

public sealed record ChangeOrganizationMemberRoleRequest(OrganizationRole Role);
