using Domora.Application.Common.Persistence;
using Domora.Domain.Organizations;
using Domora.Domain.Organizations.ValueObjects;

namespace Domora.Application.Organizations.Commands.RegisterOrganization;

public sealed class RegisterOrganizationHandler
{
    private readonly IOrganizationRepository _organizationRepository;

    private readonly IUnitOfWork _unitOfWork;

    public RegisterOrganizationHandler(
        IOrganizationRepository organizationRepository,
        IUnitOfWork unitOfWork
        )
    {
        _organizationRepository = organizationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<RegisterOrganizationResponse> Handle(RegisterOrganizationCommand command, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Handling RegisterOrganizationCommand for organization name: {command.Name}");
        
        var organizationName = OrganizationName.Create(command.Name);

        var organization = Organization.Register(organizationName);

        await _organizationRepository.AddAsync(organization, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RegisterOrganizationResponse(organization.Id, organization.Name.Value);
    }
}