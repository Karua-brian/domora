using Domora.Domain.Common;

namespace Domora.API.Leases;

public sealed record RegisterLeaseRequest
(
    Guid UnitId, 
    Guid TenantId, 
    DateOnly StartDate,
    decimal MonthlyRent,
    string Currency
);