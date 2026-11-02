using Domora.Domain.Organizations.Enums;

namespace Domora.API.Organizations;

public sealed record AddOrganizationMemberRequest(Guid UserId, OrganizationRole Role);
