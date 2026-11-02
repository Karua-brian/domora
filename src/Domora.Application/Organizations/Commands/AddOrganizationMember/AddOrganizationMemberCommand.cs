using Domora.Domain.Organizations.Enums;

namespace Domora.Application.Organizations.Commands.AddOrganizationMember;

public sealed record AddOrganizationMemberCommand(Guid UserId, OrganizationRole Role);
