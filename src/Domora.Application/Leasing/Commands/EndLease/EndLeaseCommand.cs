namespace Domora.Application.Leasing.Commands.EndLease;

public sealed record EndLeaseCommand(
    Guid LeaseId,
    DateOnly EndDate
);