using Domora.Domain.Common;
using Domora.Domain.Leasing.Enums;

namespace Domora.Domain.Leasing;

public class Lease
{
    public Guid Id { get; }

    public Guid UnitId { get; }

    public Guid TenantId { get; }

    public DateOnly StartDate { get; } 

    public DateOnly? EndDate { get; private set;} 

    public LeaseStatus Status { get; private set;}

    public Money MonthlyRent { get; } 

    public Guid Version { get; private set; }

    private Lease()
    {
        MonthlyRent = null;
    }

    private Lease(
        Guid id, 
        Guid unitId, 
        Guid tenantId, 
        DateOnly startDate, 
        Money monthlyRent,
        LeaseStatus status
        ) 
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
        Status = status;

        
        Version = Guid.NewGuid();
    }

    public static Lease Register(
        Guid unitId, 
        Guid tenantId, 
        DateOnly startDate, 
        Money monthlyRent
        )
    {
        return new Lease(
            Guid.NewGuid(), 
            unitId, 
            tenantId, 
            startDate, 
            monthlyRent,
            LeaseStatus.Active
        );    
    }

    public void EndLease(DateOnly endDate)
    {
        if (Status == LeaseStatus.Ended)
            throw new InvalidOperationException(
                "Lease has already ended."
            );
        
        if (endDate < StartDate)
            throw new ArgumentException(
                "End date cannot be earlier than start date.",
                nameof(endDate)
            );

        Status = LeaseStatus.Ended;
        EndDate = endDate;
        Version = Guid.NewGuid();
    }
}   