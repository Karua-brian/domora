using Domora.Application.Common.Exceptions;
using Domora.Application.Common.Persistence;
using Domora.Application.Organizations.Commands;
using Domora.Domain.Common.Exceptions;
using Domora.Domain.Organizations.Enums;

namespace Domora.Application.Organizations.Commands.LeaveOrganization;

public sealed class LeaveOrganizationHandler
{
    private readonly OwnershipMutationGuard _ownershipGuard;
    private readonly IOrganizationMembershipRepository _memberships;
    private readonly IUnitOfWork _unitOfWork;

    public LeaveOrganizationHandler(
        OwnershipMutationGuard ownershipGuard,
        IOrganizationMembershipRepository memberships,
        IUnitOfWork unitOfWork)
    {
        _ownershipGuard = ownershipGuard;
        _memberships = memberships;
        _unitOfWork = unitOfWork;
    }

    public async Task<LeaveOrganizationResponse> Handle(
        LeaveOrganizationCommand command,
        CancellationToken cancellationToken)
    {
        var caller = _ownershipGuard.GetCaller();
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        var organization = await _ownershipGuard.LockAndEnsureSingleOwnerAsync(
            caller,
            cancellationToken);
        var membership = await _memberships.GetByUserAsync(
            caller.ActorUserId,
            organization.OrganizationId,
            cancellationToken);

        if (membership is null)
            throw new NotFoundException("Organization membership not found.");

        if (membership.Role == OrganizationRole.Owner)
            throw new ResourceConflictException(
                "Ownership must be transferred before the Owner can leave.");

        if (membership.Role is not (OrganizationRole.Member or OrganizationRole.Finance))
            throw new ResourceConflictException(
                "This membership cannot leave the organization.");

        await _memberships.RemoveAsync(membership, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new LeaveOrganizationResponse(
            organization.OrganizationId,
            caller.ActorUserId);
    }
}
