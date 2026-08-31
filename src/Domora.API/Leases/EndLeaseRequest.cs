namespace Domora.API.Leases;

public sealed record EndLeaseRequest(
    DateOnly EndDate
);