using Domora.Application.Common.Context;
using Domora.Application.Common.Exceptions;
using Domora.Application.Common.Persistence;
using Domora.Domain.Organizations.Enums;

namespace Domora.Application.Common.Authorization;

public sealed class FinancialAuthorization : IFinancialAuthorization
{
    private readonly IUserContext _userContext;
    private readonly IOrganizationContext _organizationContext;
    private readonly IOrganizationMembershipRepository _memberships;

    public FinancialAuthorization(
        IUserContext userContext,
        IOrganizationContext organizationContext,
        IOrganizationMembershipRepository memberships
    )
    {
        _userContext = userContext;
        _organizationContext = organizationContext;
        _memberships = memberships;
    }

    public async Task EnsureCanAsync(
        FinancialCapability capability,
        CancellationToken cancellationToken = default
    )
    {
        if (!Enum.IsDefined(capability))
            throw new ForbiddenException("Financial authorization is required.");

        Guid userId;
        try
        {
            userId = _userContext.UserId;
        }
        catch (InvalidOperationException)
        {
            throw new ForbiddenException("Financial authorization is required.");
        }

        var organizationId = _organizationContext.OrganizationId;
        if (userId == Guid.Empty || organizationId == Guid.Empty)
            throw new ForbiddenException("Financial authorization is required.");

        if (await _memberships.HasRoleAsync(
                userId,
                organizationId,
                OrganizationRole.Owner,
                cancellationToken))
            return;

        var financeRoleCanPerform = capability is FinancialCapability.ReceivePayments
            or FinancialCapability.AllocatePayments
            or FinancialCapability.IssueInvoices;

        if (financeRoleCanPerform &&
            await _memberships.HasRoleAsync(
                userId,
                organizationId,
                OrganizationRole.Finance,
                cancellationToken))
            return;

        throw new ForbiddenException(
            "You are not authorized to perform this financial operation."
        );
    }
}
