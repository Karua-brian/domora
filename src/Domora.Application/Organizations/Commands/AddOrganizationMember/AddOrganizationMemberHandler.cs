using Domora.Application.Common.Exceptions;
using Domora.Application.Common.Persistence;
using Domora.Application.Organizations.Commands;
using Domora.Domain.Common.Exceptions;
using Domora.Domain.Organizations;
using Domora.Domain.Organizations.Enums;

namespace Domora.Application.Organizations.Commands.AddOrganizationMember;

public sealed class AddOrganizationMemberHandler
{
    private readonly OwnershipMutationGuard _ownershipGuard;
    private readonly IOrganizationMembershipRepository _memberships;
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;

    public AddOrganizationMemberHandler(
        OwnershipMutationGuard ownershipGuard,
        IOrganizationMembershipRepository memberships,
        IUserRepository users,
        IUnitOfWork unitOfWork)
    {
        _ownershipGuard = ownershipGuard;
        _memberships = memberships;
        _users = users;
        _unitOfWork = unitOfWork;
    }

    public async Task<AddOrganizationMemberResponse> Handle(
        AddOrganizationMemberCommand command,
        CancellationToken cancellationToken)
    {
        ValidateRole(command.Role);
        if (command.UserId == Guid.Empty)
            throw new DomainValidationException("User ID is required.");

        var caller = _ownershipGuard.GetCaller();
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        var ownership = await _ownershipGuard.LockAndRequireOwnerAsync(caller, cancellationToken);

        if (!await _users.ExistsAsync(command.UserId, cancellationToken))
            throw new NotFoundException("User not found.");

        if (await _memberships.GetByUserAsync(
                command.UserId,
                ownership.OrganizationId,
                cancellationToken) is not null)
            throw new ResourceConflictException(
                "The user is already a member of this organization.");

        var membership = OrganizationMembership.Create(
            command.UserId,
            ownership.OrganizationId,
            command.Role);
        await _memberships.AddAsync(membership, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new AddOrganizationMemberResponse(
            ownership.OrganizationId,
            membership.UserId,
            membership.Role);
    }

    private static void ValidateRole(OrganizationRole role)
    {
        if (role is not (OrganizationRole.Member or OrganizationRole.Finance))
            throw new DomainValidationException(
                "New memberships must have the Member or Finance role.");
    }
}
