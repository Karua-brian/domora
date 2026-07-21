using Domora.Domain.Organizations;
using Domora.Domain.Organizations.ValueObjects;

namespace Domora.Application.Organizations.Commands.RegisterOrganization;

public sealed class RegisterOrganizationHandler
{
    private readonly IOrganizationRepository _organizationRepository;

    public RegisterOrganizationHandler(IOrganizationRepository organizationRepository)
    {
        _organizationRepository = organizationRepository;
    }

    public async Task<RegisterOrganizationResponse> Handle(RegisterOrganizationCommand command, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Handling RegisterOrganizationCommand for organization name: {command.Name}");
        
        var name = OrganizationName.Create(command.Name);

        var organization = Organization.Register(name);

        await _organizationRepository.AddAsync(organization, cancellationToken);

        return new RegisterOrganizationResponse(organization.Id, organization.Name.Value);
    }
}