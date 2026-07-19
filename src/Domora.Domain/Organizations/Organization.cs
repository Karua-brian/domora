namespace Domora.Domain.Organizations;

using Domora.Domain.Organizations.ValueObjects;
 
// Represents an organization that manages properties. 
public class Organization
{

    public Guid Id { get;}

    public OrganizationName Name { get; } 
    
    public Organization(Guid id, OrganizationName name)
    {
        Id = id,
        Name = name;
    }
}