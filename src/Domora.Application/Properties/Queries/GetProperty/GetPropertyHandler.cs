using Domora.Application.Common.Context;
using Domora.Application.Common.Exceptions;
using Domora.Domain.Properties;

namespace Domora.Application.Properties.Queries.GetProperty;

public sealed class GetPropertyHandler
{
    private readonly IPropertyRepository _propertyRepository;

    private readonly IOrganizationContext _organizationContext;

    public GetPropertyHandler(
        IPropertyRepository propertyRepository,
        IOrganizationContext organizationContext
    )
    {
        _propertyRepository = propertyRepository;
        _organizationContext = organizationContext;
    }

    public async Task<GetPropertyResponse> Handle(
        GetPropertyQuery query,
        CancellationToken cancellationToken
    )
    {
        var organizationId = _organizationContext.OrganizationId;

        var property = await _propertyRepository.GetByIdAsync(
            query.PropertyId,
            organizationId,
            cancellationToken
        );

        if (property is null)
            throw new NotFoundException(
                "Property not found."
            );

        return new GetPropertyResponse(
            property.Id,
            property.OrganizationId,
            property.Name.Value
        );
    }

}