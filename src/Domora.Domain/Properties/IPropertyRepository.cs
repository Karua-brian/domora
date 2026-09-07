namespace Domora.Domain.Properties;

public interface IPropertyRepository
{
    Task AddAsync(
        Property property, 
        CancellationToken cancellationToken = default
    );

    Task<Property?> GetByIdAsync(
        Guid propertyId,
        Guid organizationId,
        CancellationToken cancellationToken = default
    );
}