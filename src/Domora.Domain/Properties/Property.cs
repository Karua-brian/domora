namespace Domora.Domain.Properties;

using Domora.Domain.Properties.ValueObjects;
using Domora.Domain.Common.Exceptions;


// Represents a physical real estate asset managed by an Organization.
// Business rules and relationships will be added only as they are discovered.
public class Property
{
    public Guid Id { get; }

    public Guid OrganizationId { get; }

    public PropertyName Name { get; }

    private Property(
        Guid id, 
        Guid organizationId, 
        PropertyName name
        )
    {
        if (id == Guid.Empty)
            throw new DomainValidationException(
                "Property ID is required."
                );

        if (organizationId == Guid.Empty)
            throw new DomainValidationException(
                "Organization ID is required."
                );
                
        Id = id;
        Name = name;
        OrganizationId = organizationId;
    }

    public static Property Register(
        Guid organizationId, 
        PropertyName name
        )
    {
        return new Property(
            Guid.NewGuid(), 
            organizationId, 
            name
        );
    }
}

