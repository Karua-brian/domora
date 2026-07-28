using Domora.Domain.Common;

namespace Domora.Application.Leasing.Commands.RegisterLease;

public sealed record RegisterLeaseResponse
(
    Guid Id,
    Guid UnitId,
    Guid TenantId,
    DateOnly StartDate,
    decimal MonthlyRent
);