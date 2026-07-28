using Domora.Domain.Common;

namespace Domora.Domain.Leasing;

public class Lease
{
    public Guid Id { get; }

    public Guid UnitId { get; }

    public Guid TenantId { get; }

    public DateOnly StartDate { get; }

    public Money MonthlyRent { get; } 

    private Lease()
    {
        MonthlyRent = null;
    }

    private Lease(Guid id, Guid unitId, Guid tenantId, DateOnly startDate, Money monthlyRent) 
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Lease ID is required.", nameof(id));

        if (unitId == Guid.Empty)
            throw new ArgumentException("Unit ID is required.", nameof(unitId));

        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID is required.", nameof(tenantId));

        Id = id;
        UnitId = unitId;    
        TenantId = tenantId;
        StartDate = startDate;
        MonthlyRent = monthlyRent;
    }

    public static Lease Register(Guid unitId, Guid tenantId, DateOnly startDate, Money monthlyRent)
    {
        return new Lease(Guid.NewGuid(), unitId, tenantId, startDate, monthlyRent);    
    }
}   