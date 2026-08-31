using Domora.Domain.Leasing.Enums;

namespace Domora.Application.Leasing.Commands.EndLease;

public sealed record EndLeaseResponse(
    Guid Id,
    Guid UnitId,
    Guid TenantId,
    DateOnly StartDate,
    DateOnly EndDate,
    LeaseStatus Status,
    Guid Version
);