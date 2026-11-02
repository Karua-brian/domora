using Domora.Application.Common.Exceptions;
using Domora.Application.Common.Persistence;
using Domora.Application.Organizations.Commands;
using Domora.Domain.Common.Exceptions;
using Domora.Domain.Organizations.Enums;

namespace Domora.Application.Organizations.Commands.RemoveOrganizationMember;

public sealed class RemoveOrganizationMemberHandler
{
    private readonly OwnershipMutationGuard _ownershipGuard;
    private readonly IOrganizationMembershipRepository _memberships;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveOrganizationMemberHandler(
        OwnershipMutationGuard ownershipGuard,
        IOrganizationMembershipRepository memberships,
        IUnitOfWork unitOfWork)
    {
        _ownershipGuard = ownershipGuard;
        _memberships = memberships;
        _unitOfWork = unitOfWork;
    }

    public async Task<RemoveOrganizationMemberResponse> Handle(
        RemoveOrganizationMemberCommand command,
        CancellationToken cancellationToken)
    {
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
                "Ownership must be transferred before the Owner can be removed.");

        await _memberships.RemoveAsync(membership, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new RemoveOrganizationMemberResponse(
            ownership.OrganizationId,
            membership.UserId);
    }
}
