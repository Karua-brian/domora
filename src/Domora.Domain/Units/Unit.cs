namespace Domora.Domain.Units;

using Domora.Domain.Units.ValueObjects;

public class Unit 
{
    public Guid Id { get;}

    public UnitNumber Number { get;}

    public Guid PropertyId { get:}

    public Unit(Guid id, UnitNumber number, Guid propertyId)
    {
        if (id == Guid.Empty)
            throw new ArguementException("Unit ID is required.", nameof(id));

        if (Guid.Empty == propertyId)
            throw new ArguementException("Property ID is required.", nameof(propertyId));

        Id = id;
        Number = number;
        PropertyId = propertyId;        
    }
}