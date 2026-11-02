using Domora.Domain.Common.Exceptions;
using Domora.Domain.Organizations.Enums;

namespace Domora.Domain.Organizations;

public sealed class OrganizationMembership
{
    public Guid Id { get; }

    public Guid UserId { get; }

    public Guid OrganizationId { get; }

    public OrganizationRole Role { get; private set; }

    private OrganizationMembership(
        Guid id,
        Guid userId,
        Guid organizationId,
        OrganizationRole role
    )
    {
        Id = id;
        UserId = userId;
        OrganizationId = organizationId;
        Role = role;
    }

    public static OrganizationMembership Create(
        Guid userId,
        Guid organizationId,
        OrganizationRole role
    )
    {
        if (userId == Guid.Empty)
            throw new DomainValidationException(
                "User ID is required."
            );

        if (organizationId == Guid.Empty)
            throw new DomainValidationException(
                "Organization ID is required."
            );

        if (!Enum.IsDefined(role))
            throw new DomainValidationException(
                "Organization role is invalid."
            );

        return new OrganizationMembership(
            Guid.NewGuid(),
            userId,
            organizationId,
            role
        );
    }

    public void TransferOwnershipTo(OrganizationMembership target)
    {
        ValidateTransferTarget(target);
        Role = OrganizationRole.Finance;
        target.Role = OrganizationRole.Owner;
    }

    public void DemoteForOwnershipTransferTo(OrganizationMembership target)
    {
        ValidateTransferTarget(target);
        Role = OrganizationRole.Finance;
    }

    public void PromoteAfterOwnershipTransferOf(OrganizationMembership formerOwner)
    {
        ArgumentNullException.ThrowIfNull(formerOwner);

        if (OrganizationId != formerOwner.OrganizationId)
            throw new DomainValidationException(
                "The new Owner must belong to the same organization."
            );

        if (formerOwner.Role != OrganizationRole.Finance)
            throw new DomainValidationException(
                "The former Owner must be demoted before ownership is transferred."
            );

        if (Role is not (OrganizationRole.Member or OrganizationRole.Finance))
            throw new DomainValidationException(
                "Only a non-Owner member can receive ownership."
            );

        Role = OrganizationRole.Owner;
    }

    private void ValidateTransferTarget(OrganizationMembership target)
    {
        ArgumentNullException.ThrowIfNull(target);

        if (Role != OrganizationRole.Owner)
            throw new DomainValidationException(
                "Only the current Owner can transfer ownership."
            );

        if (target.OrganizationId != OrganizationId)
            throw new DomainValidationException(
                "The new Owner must belong to the same organization."
            );

        if (target.Id == Id)
            throw new DomainValidationException(
                "The current Owner cannot transfer ownership to themselves."
            );

        if (target.Role == OrganizationRole.Owner)
            throw new DomainValidationException(
                "The target is already an Owner."
            );
    }

    public void ChangeRole(OrganizationRole role)
    {
        if (Role == OrganizationRole.Owner)
            throw new DomainValidationException(
                "The Owner role can only change through ownership transfer."
            );

        if (role is not (OrganizationRole.Member or OrganizationRole.Finance))
            throw new DomainValidationException(
                "Membership role must be Member or Finance."
            );

        Role = role;
    }
}
