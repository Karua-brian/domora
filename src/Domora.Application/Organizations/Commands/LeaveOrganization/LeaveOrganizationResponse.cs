namespace Domora.Application.Organizations.Commands.LeaveOrganization;

public sealed record LeaveOrganizationResponse(Guid OrganizationId, Guid UserId);
