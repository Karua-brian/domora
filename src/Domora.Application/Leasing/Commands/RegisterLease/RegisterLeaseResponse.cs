using Domora.Domain.Common;
using Domora.Domain.Leasing.Enums;

namespace Domora.Application.Leasing.Commands.RegisterLease;

public sealed record RegisterLeaseResponse
(
    Guid Id,
    Guid UnitId,
    Guid TenantId,
    DateOnly StartDate,
    decimal MonthlyRent,
    string Currency,
    LeaseStatus Status,
    Guid Version
);