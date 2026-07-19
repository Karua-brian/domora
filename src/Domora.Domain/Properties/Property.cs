namespace Domora.Domain.Properties;

using Domora.Domain.Properties.ValueObjects;

// Represents a physical real estate asset managed by an Organization.
// Business rules and relationships will be added only as they are discovered.
public class Property
{
    public Guid Id { get; }

    public PropertyName Name { get; }

    public Guid OrganizationId { get; }

    public Property(Guid id, PropertyName name, Guid organizationId)
    {
        if (id == Guid.Empty)
            throw new ArguementException("Property ID is required.", nameof(id));

        if (organizationId == Guid.Empty)
            throw new ArguementException("Organization ID is required.", nameof(organizationId));
                
        Id = id;
        Name = name;
        OrganizationId = organizationId;
    }
}

