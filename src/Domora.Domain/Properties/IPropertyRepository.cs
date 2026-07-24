namespace Domora.Domain.Properties;

public interface IPropertyRepository
{
    Task AddAsync(Property property, CancellationToken cancellationToken = default);
}