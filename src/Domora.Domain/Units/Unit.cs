namespace Domora.Domain.Units;

using Domora.Domain.Common.Exceptions;
using Domora.Domain.Units.Enums;
using Domora.Domain.Units.ValueObjects;

public class Unit 
{
    public Guid Id { get; }

    public Guid PropertyId { get; }

    public UnitNumber Number { get; }

    public UnitType Type { get; }

    public OccupancyStatus Status { get; private set; }

    public Guid Version { get; private set; }

    private Unit(
        Guid id, 
        Guid propertyId, 
        UnitNumber number, 
        UnitType type, 
        OccupancyStatus status
        )
    {
        if (id == Guid.Empty)
            throw new DomainValidationException("Unit ID is required.");

        if (Guid.Empty == propertyId)
            throw new DomainValidationException("Property ID is required.");

        Id = id;
        PropertyId = propertyId;
        Number = number;
        Type = type;
        Status = status;
        Version = Guid.NewGuid();
    }

    public static Unit Register(
        Guid propertyId, 
        UnitNumber number, 
        UnitType type   
        )
    {
        return new Unit(
            Guid.NewGuid(), 
            propertyId, 
            number, 
            type, 
            OccupancyStatus.Vacant
            );
    }

    public void Occupy()
    {
        if (Status == OccupancyStatus.Occupied)
            throw new ResourceConflictException(
                "Unit is already occupied."
            );

        Status = OccupancyStatus.Occupied;
        Version = Guid.NewGuid();
    }

    public void Vacate()
    {
        if (Status == OccupancyStatus.Vacant)
            throw new ResourceConflictException(
                "Unit is already vacant."
        );

        Status = OccupancyStatus.Vacant;
        Version = Guid.NewGuid();
    }

    public void UnderMaintaenance()
    {
        if (Status == OccupancyStatus.UnderMaintenance)
            throw new ResourceConflictException(
                "Unit is already under maintenance."
        );

        Status = OccupancyStatus.UnderMaintenance;
        Version = Guid.NewGuid();   
    }
}