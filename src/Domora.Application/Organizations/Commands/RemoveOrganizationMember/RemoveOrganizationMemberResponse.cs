namespace Domora.Application.Organizations.Commands.RemoveOrganizationMember;

public sealed record RemoveOrganizationMemberResponse(Guid OrganizationId, Guid UserId);
