using Domora.Domain.Organizations.Enums;

namespace Domora.Application.Organizations.Commands.AddOrganizationMember;

public sealed record AddOrganizationMemberResponse(
    Guid OrganizationId,
    Guid UserId,
    OrganizationRole Role);
