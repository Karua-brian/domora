using Domora.Application.Common.Context;
using Domora.Application.Common.Exceptions;
using Domora.Application.Common.Persistence;
using Domora.Domain.Common.Exceptions;
using Domora.Domain.Organizations;

namespace Domora.Application.Organizations.Commands;

/// <summary>
/// Shared guard for ownership-sensitive membership mutations. Call only after
/// starting the unit-of-work transaction so the organization row lock is held
/// through the caller's save and commit.
/// </summary>
public sealed class OwnershipMutationGuard
{
    private readonly IUserContext _userContext;
    private readonly IOrganizationContext _organizationContext;
    private readonly IOrganizationMembershipRepository _memberships;

    public OwnershipMutationGuard(
        IUserContext userContext,
        IOrganizationContext organizationContext,
        IOrganizationMembershipRepository memberships)
    {
        _userContext = userContext;
        _organizationContext = organizationContext;
        _memberships = memberships;
    }

    public OwnershipMutationCaller GetCaller()
    {
        Guid actorUserId;
        try
        {
            actorUserId = _userContext.UserId;
        }
        catch (InvalidOperationException)
        {
            throw new ForbiddenException("Organization Owner authorization is required.");
        }

        var organizationId = _organizationContext.OrganizationId;
        if (actorUserId == Guid.Empty || organizationId == Guid.Empty)
            throw new ForbiddenException("Organization Owner authorization is required.");

        return new OwnershipMutationCaller(organizationId, actorUserId);
    }

    public async Task<OwnershipMutationContext> LockAndRequireOwnerAsync(
        OwnershipMutationCaller caller,
        CancellationToken cancellationToken)
    {
        var ownership = await LockAndEnsureSingleOwnerAsync(caller, cancellationToken);

        if (ownership.OwnerMembership.UserId != caller.ActorUserId)
            throw new ForbiddenException("Only the current Owner can manage membership.");

        return ownership;
    }

    public async Task<OwnershipMutationContext> LockAndEnsureSingleOwnerAsync(
        OwnershipMutationCaller caller,
        CancellationToken cancellationToken)
    {
        await _memberships.LockOrganizationAsync(caller.OrganizationId, cancellationToken);
        var owners = await _memberships.GetOwnersAsync(caller.OrganizationId, cancellationToken);

        if (owners.Count != 1)
            throw new ResourceConflictException(
                "The organization does not have exactly one Owner.");

        return new OwnershipMutationContext(caller.OrganizationId, caller.ActorUserId, owners[0]);
    }
}

public sealed record OwnershipMutationContext(
    Guid OrganizationId,
    Guid ActorUserId,
    OrganizationMembership OwnerMembership);

public sealed record OwnershipMutationCaller(
    Guid OrganizationId, Guid ActorUserId);
