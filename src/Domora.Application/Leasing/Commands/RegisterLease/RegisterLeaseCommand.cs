using Domora.Domain.Common;

namespace Domora.Application.Leasing.Commands.RegisterLease;

public sealed record RegisterLeaseCommand
(
    Guid UnitId, 
    Guid TenantId, 
    DateOnly StartDate, 
    Money MonthlyRent
    );