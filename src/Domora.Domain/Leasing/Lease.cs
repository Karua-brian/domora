using Domora.Domain.Common;
using Domora.Domain.Leasing.Enums;
using Domora.Domain.Common.Exceptions;


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
            throw new DomainValidationException("Lease ID is required.");

        if (unitId == Guid.Empty)
            throw new DomainValidationException("Unit ID is required.");

        if (tenantId == Guid.Empty)
            throw new DomainValidationException("Tenant ID is required.");

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
        Money monthlyRent
        )
    {
        return new Lease(
            Guid.NewGuid(), 
            unitId, 
            tenantId, 
            DateOnly.FromDateTime(DateTime.UtcNow), 
            monthlyRent,
            LeaseStatus.Active
        );    
    }

    public void EndLease(DateOnly endDate)
    {
        if (Status == LeaseStatus.Ended)
            throw new ResourceConflictException(
                "Lease has already ended."
            );
        
        if (endDate < StartDate)
            throw new DomainValidationException(
                "End date cannot be earlier than start date."
            );

        Status = LeaseStatus.Ended;
        EndDate = endDate;
        Version = Guid.NewGuid();
    }
}   