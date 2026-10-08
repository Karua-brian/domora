using Domora.Domain.Common.Exceptions;
using Domora.Domain.Organizations.Enums;

namespace Domora.Domain.Organizations;

public sealed class OrganizationMembership
{
    public Guid Id { get; }

    public Guid UserId { get; }

    public Guid OrganizationId { get; }

    public OrganizationRole Role { get; }

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
}