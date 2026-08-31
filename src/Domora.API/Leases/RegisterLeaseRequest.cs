using System.ComponentModel.DataAnnotations;

namespace Domora.API.Leases;

public sealed record RegisterLeaseRequest
(
    Guid UnitId, 
    Guid TenantId, 

    [Range(0.01, double.MaxValue)]
    decimal MonthlyRent,

    [Required]
    string Currency
);