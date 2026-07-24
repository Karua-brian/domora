namespace Domora.Domain.Organizations;

using Domora.Domain.Organizations.ValueObjects;
 
// Represents an organization that manages properties. 
public class Organization
{
    public Guid Id { get;}

    public OrganizationName Name { get; } 
    
    private Organization(Guid id, OrganizationName name)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Organization ID is required.", nameof(id));

        Id = id;
        Name = name;
    }

    public static Organization Register(OrganizationName name)
    {

        return new Organization(Guid.NewGuid(), name);
    }
}