using Domora.Application.Common.Persistence;
using Domora.Domain.Properties;
using Domora.Domain.Properties.ValueObjects;

namespace Domora.Application.Properties.Commands.RegisterProperty;

public sealed class RegisterPropertyHandler
{
    private readonly IPropertyRepository _propertyRepository;

    private readonly IUnitOfWork _unitOfWork;

    public RegisterPropertyHandler(
        IPropertyRepository propertyRepository,
        IUnitOfWork unitOfWork
        )
    {
        _propertyRepository = propertyRepository;
        _unitOfWork = unitOfWork;

    }

    public async Task<RegisterPropertyResponse> Handle(RegisterPropertyCommand command, CancellationToken cancellationToken)
    {
        var propertyName = PropertyName.Create(command.Name);

        var property = Property.Register(command.PropertyId, propertyName);

        await _propertyRepository.AddAsync(property, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RegisterPropertyResponse(property.Id, property.OrganizationId, property.Name.Value);
    }
}