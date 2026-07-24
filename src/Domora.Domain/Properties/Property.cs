namespace Domora.Domain.Properties;
using Domora.Domain.Properties.ValueObjects;

// Represents a physical real estate asset managed by an Organization.
// Business rules and relationships will be added only as they are discovered.
public class Property
{
    public Guid Id { get; }

    public Guid OrganizationId { get; }

    public PropertyName Name { get; }

    public Property(Guid id, Guid organizationId, PropertyName name)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Property ID is required.", nameof(id));

        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization ID is required.", nameof(organizationId));
                
        Id = id;
        Name = name;
        OrganizationId = organizationId;
    }

    public static Property Register(Guid organizationId, PropertyName name)
    {
        return new Property(Guid.NewGuid(), organizationId, name);
    }
}

