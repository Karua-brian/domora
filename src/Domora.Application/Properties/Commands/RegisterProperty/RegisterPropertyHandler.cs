using Domora.Application.Common.Context;
using Domora.Application.Common.Persistence;
using Domora.Domain.Properties;
using Domora.Domain.Properties.ValueObjects;

namespace Domora.Application.Properties.Commands.RegisterProperty;

public sealed class RegisterPropertyHandler
{
    private readonly IPropertyRepository _propertyRepository;

    private readonly IOrganizationContext _organizationContext;

    private readonly IUnitOfWork _unitOfWork;

    public RegisterPropertyHandler(
        IPropertyRepository propertyRepository,
        IOrganizationContext organizationContext,
        IUnitOfWork unitOfWork
        )
    {
        _propertyRepository = propertyRepository;
        _organizationContext = organizationContext;
        _unitOfWork = unitOfWork;

    }

    public async Task<RegisterPropertyResponse> Handle(
        RegisterPropertyCommand command, 
        CancellationToken cancellationToken
    )
    {
        var organizationId = _organizationContext.OrganizationId;

        var propertyName = PropertyName.Create(command.Name);

        var property = Property.Register(
            organizationId,
            propertyName
        );

        await _propertyRepository.AddAsync(
            property, 
            cancellationToken
        );

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RegisterPropertyResponse(
            property.Id, 
            property.OrganizationId, 
            property.Name.Value
        );
    }
}