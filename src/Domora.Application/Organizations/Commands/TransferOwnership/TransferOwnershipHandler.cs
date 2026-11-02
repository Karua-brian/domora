using Domora.Application.Common.Exceptions;
using Domora.Application.Common.Persistence;
using Domora.Domain.Common.Exceptions;
using Domora.Domain.Organizations.Enums;
using Domora.Application.Organizations.Commands;

namespace Domora.Application.Organizations.Commands.TransferOwnership;

public sealed class TransferOwnershipHandler
{
    private readonly OwnershipMutationGuard _ownershipGuard;
    private readonly IOrganizationMembershipRepository _memberships;
    private readonly IUnitOfWork _unitOfWork;

    public TransferOwnershipHandler(
        OwnershipMutationGuard ownershipGuard,
        IOrganizationMembershipRepository memberships,
        IUnitOfWork unitOfWork)
    {
        _ownershipGuard = ownershipGuard;
        _memberships = memberships;
        _unitOfWork = unitOfWork;
    }

    public async Task<TransferOwnershipResponse> Handle(
        TransferOwnershipCommand command,
        CancellationToken cancellationToken)
    {
        if (command.NewOwnerUserId == Guid.Empty)
            throw new DomainValidationException("New Owner user ID is required.");

        var caller = _ownershipGuard.GetCaller();

        await using var transaction = await _unitOfWork.BeginTransactionAsync(
            cancellationToken);

        var ownership = await _ownershipGuard.LockAndRequireOwnerAsync(caller, cancellationToken);
        var organizationId = ownership.OrganizationId;
        var actorUserId = ownership.ActorUserId;
        var currentOwner = ownership.OwnerMembership;

        var target = await _memberships.GetByUserAsync(
            command.NewOwnerUserId,
            organizationId,
            cancellationToken);

        if (target is null)
            throw new NotFoundException(
                "The new Owner must already be a member of this organization.");

        if (target.Role == OrganizationRole.Owner)
            throw new ResourceConflictException(
                "The target is already the Owner.");

        currentOwner.DemoteForOwnershipTransferTo(target);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        target.PromoteAfterOwnershipTransferOf(currentOwner);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new TransferOwnershipResponse(
            organizationId,
            actorUserId,
            target.UserId);
    }
}
