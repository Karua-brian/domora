using Domora.Application.Common.Authorization;
using Domora.Application.Common.Context;
using Domora.Application.Common.Exceptions;
using Domora.Application.Common.Persistence;
using Domora.Domain.Organizations;
using Domora.Domain.Organizations.Enums;

namespace Domora.Infrastructure.Tests.Security;

public sealed class FinancialAuthorizationRoleTests
{
    private sealed class TestContext(Guid organizationId) : IOrganizationContext, IUserContext
    {
        public Guid OrganizationId { get; private set; } = organizationId;
        public Guid UserId { get; init; }
        public void Set(Guid value) => OrganizationId = value;
    }

    private sealed class UnknownRoleMembershipRepository : IOrganizationMembershipRepository
    {
        private const string PersistedRole = "UnrecognizedRole";

        public Task<bool> HasRoleAsync(
            Guid userId,
            Guid organizationId,
            OrganizationRole role,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(string.Equals(
                PersistedRole,
                role.ToString(),
                StringComparison.Ordinal));

        public Task AddAsync(OrganizationMembership membership, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task RemoveAsync(OrganizationMembership membership, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> ExistsAsync(Guid userId, Guid organizationId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task LockOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<OrganizationMembership>> GetOwnersAsync(Guid organizationId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<OrganizationMembership?> GetByUserAsync(Guid userId, Guid organizationId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    [Fact]
    public async Task Unknown_persisted_role_fails_closed_for_financial_authorization()
    {
        var context = new TestContext(Guid.NewGuid()) { UserId = Guid.NewGuid() };
        // PostgreSQL now rejects this persisted role. Simulate the stored value here
        // to preserve coverage that unknown role text grants no financial capability.
        var memberships = new UnknownRoleMembershipRepository();

        Assert.False(await memberships.HasRoleAsync(
            context.UserId, context.OrganizationId,
            OrganizationRole.Owner, CancellationToken.None));
        Assert.False(await memberships.HasRoleAsync(
            context.UserId, context.OrganizationId,
            OrganizationRole.Finance, CancellationToken.None));

        var authorization = new FinancialAuthorization(context, context, memberships);
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            authorization.EnsureCanAsync(FinancialCapability.ReceivePayments));
    }
}
