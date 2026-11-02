namespace Domora.Application.Organizations.Commands.TransferOwnership;

public sealed record TransferOwnershipResponse(
    Guid OrganizationId,
    Guid FormerOwnerUserId,
    Guid NewOwnerUserId);
