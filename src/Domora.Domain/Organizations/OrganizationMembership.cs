namespace Domora.Domain.Organizations;

public sealed class OrganizationMembership
{
    public Guid Id { get; }

    public Guid UserId { get; }

    public Guid OrganizationId { get; }

    private OrganizationMembership(
        Guid id,
        Guid userId,
        Guid organizationId
    )
    {
        Id = id;
        UserId = userId;
        OrganizationId = organizationId;
    }

    public static OrganizationMembership Create(
        Guid userId,
        Guid organizationId
    )
    {
        if (userId == Guid.Empty)
            throw new ArgumentException(
                "User ID is required.",
                nameof(userId)
            );

        if (organizationId == Guid.Empty)
            throw new ArgumentException(
                "Organization ID is required.",
                nameof(organizationId)
            );

        return new OrganizationMembership(
            Guid.NewGuid(),
            userId,
            organizationId
        );
    }
}