using Domora.Domain.Organizations;

namespace Domora.Application.Organizations.Commands.RegisterOrganization;

public sealed class RegisterOrganizationHandler
{
    private readonly IOrganizationRepository _organizationRepository;

    public RegisterOrganizationHandler(IOrganizationRepository organizationRepository)
    {
        _organizationRepository = organizationRepository;
    }

    public async Task Handle(RegisterOrganizationCommand command)
    {
        var organization = Organization.Register(command.Name);

        await _organizationRepository.AddAsync(organization, cancellationToken: default);
    }
}