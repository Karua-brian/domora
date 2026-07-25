namespace Domora.Domain.Units;

using Domora.Domain.Units.Enums;
using Domora.Domain.Units.ValueObjects;

public class Unit 
{
    public Guid Id { get; }

    public Guid PropertyId { get; }

    public UnitNumber Number { get; }

    public UnitType Type { get; }

    public OccupancyStatus Status { get; }

    public Unit(Guid id, Guid propertyId, UnitNumber number, UnitType type, OccupancyStatus status)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Unit ID is required.", nameof(id));

        if (Guid.Empty == propertyId)
            throw new ArgumentException("Property ID is required.", nameof(propertyId));

        Id = id;
        PropertyId = propertyId;
        Number = number;
        Type = type;
        Status = status;
    }

    public static Unit Register(Guid propertyId, UnitNumber number, UnitType type)
    {
        return new Unit(Guid.NewGuid(), propertyId, number, type, OccupancyStatus.Vacant);
    }
}