using Domora.Domain.Properties;
using Domora.Domain.Properties.ValueObjects;

namespace Domora.Application.Properties.Commands.RegisterProperty;

public sealed class RegisterPropertyHandler
{
    private readonly IPropertyRepository _propertyRepository;

    public RegisterPropertyHandler(IPropertyRepository propertyRepository)
    {
        _propertyRepository = propertyRepository;
    }

    public async Task<RegisterPropertyResponse> Handle(RegisterPropertyCommand command, CancellationToken cancellationToken)
    {
        var propertyName = PropertyName.Create(command.Name);

        var property = Property.Register(command.PropertyId, propertyName);

        await _propertyRepository.AddAsync(property, cancellationToken);

        return new RegisterPropertyResponse(property.Id, property.OrganizationId, property.Name.Value);
    }
}