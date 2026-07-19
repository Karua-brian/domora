namespace Domora.Domain.Leasing;

public class Lease
{
    public Guid Id { get; }

    public Guid UnitId { get; }

    public Guid TenantId { get; }

    public DateOnly StartDate { get; }

    public Decimal MonthlyRent { get; }

    public Lease(Guid id, Guid unitId, Guid tenantId, DateOnly startDate, Decimal monthlyRent) 
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
}