using Domora.Application.Common.Exceptions;
using Domora.Application.Common.Persistence;
using Domora.Application.Organizations.Commands;
using Domora.Domain.Common.Exceptions;
using Domora.Domain.Organizations.Enums;

namespace Domora.Application.Organizations.Commands.ChangeOrganizationMemberRole;

public sealed class ChangeOrganizationMemberRoleHandler
{
    private readonly OwnershipMutationGuard _ownershipGuard;
    private readonly IOrganizationMembershipRepository _memberships;
    private readonly IUnitOfWork _unitOfWork;

    public ChangeOrganizationMemberRoleHandler(
        OwnershipMutationGuard ownershipGuard,
        IOrganizationMembershipRepository memberships,
        IUnitOfWork unitOfWork)
    {
        _ownershipGuard = ownershipGuard;
        _memberships = memberships;
        _unitOfWork = unitOfWork;
    }

    public async Task<ChangeOrganizationMemberRoleResponse> Handle(
        ChangeOrganizationMemberRoleCommand command,
        CancellationToken cancellationToken)
    {
        ValidateRole(command.Role);
        if (command.UserId == Guid.Empty)
            throw new DomainValidationException("User ID is required.");

        var caller = _ownershipGuard.GetCaller();
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        var ownership = await _ownershipGuard.LockAndRequireOwnerAsync(caller, cancellationToken);
        var membership = await _memberships.GetByUserAsync(
            command.UserId,
            ownership.OrganizationId,
            cancellationToken);

        if (membership is null)
            throw new NotFoundException("Organization membership not found.");

        if (membership.Role == OrganizationRole.Owner)
            throw new ResourceConflictException(
                "The Owner role can only change through ownership transfer.");

        membership.ChangeRole(command.Role);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new ChangeOrganizationMemberRoleResponse(
            ownership.OrganizationId,
            membership.UserId,
            membership.Role);
    }

    private static void ValidateRole(OrganizationRole role)
    {
        if (role is not (OrganizationRole.Member or OrganizationRole.Finance))
            throw new DomainValidationException(
                "Membership role must be Member or Finance.");
    }
}
