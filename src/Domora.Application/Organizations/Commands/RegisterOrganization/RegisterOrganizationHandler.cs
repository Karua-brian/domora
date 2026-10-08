using Domora.Application.Common.Context;
using Domora.Application.Common.Persistence;
using Domora.Domain.Organizations;
using Domora.Domain.Organizations.Enums;
using Domora.Domain.Organizations.ValueObjects;

namespace Domora.Application.Organizations.Commands.RegisterOrganization;

public sealed class RegisterOrganizationHandler
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IOrganizationMembershipRepository _membershipRepository;
    private readonly IUserContext _userContext;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterOrganizationHandler(
        IOrganizationRepository organizationRepository,
        IOrganizationMembershipRepository membershipRepository,
        IUserContext userContext,
        IUnitOfWork unitOfWork
        )
    {
        _organizationRepository = organizationRepository;
        _membershipRepository = membershipRepository;
        _userContext = userContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<RegisterOrganizationResponse> Handle(
        RegisterOrganizationCommand command, 
        CancellationToken cancellationToken
        )
    {
        
        var organizationName = OrganizationName.Create(
            command.Name
        );

        var organization = Organization.Register(
            organizationName
        );

        var membership = OrganizationMembership.Create(
            _userContext.UserId,
            organization.Id,
            OrganizationRole.Owner
        );

        await _organizationRepository.AddAsync(
            organization, 
            cancellationToken
        );

        await _membershipRepository.AddAsync(
            membership,
            cancellationToken
        );

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RegisterOrganizationResponse(
            organization.Id, 
            organization.Name.Value
        );
    }
}