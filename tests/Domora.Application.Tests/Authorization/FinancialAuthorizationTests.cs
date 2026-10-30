using Domora.Application.Common.Authorization;
using Domora.Application.Common.Context;
using Domora.Application.Common.Exceptions;
using Domora.Application.Common.Persistence;
using Domora.Domain.Organizations.Enums;
using Moq;

namespace Domora.Application.Tests.Authorization;

public sealed class FinancialAuthorizationTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid OrganizationId = Guid.NewGuid();

    private sealed class TestUserContext(Guid userId) : IUserContext
    {
        public Guid UserId { get; } = userId;
    }

    private sealed class TestOrganizationContext(Guid organizationId) : IOrganizationContext
    {
        public Guid OrganizationId { get; private set; } = organizationId;
        public void Set(Guid value) => OrganizationId = value;
    }

    private static FinancialAuthorization CreateAuthorization(
        OrganizationRole? role,
        out Mock<IOrganizationMembershipRepository> memberships
    )
    {
        memberships = new Mock<IOrganizationMembershipRepository>();
        memberships
            .Setup(x => x.HasRoleAsync(
                UserId,
                OrganizationId,
                It.IsAny<OrganizationRole>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, Guid _, OrganizationRole requested, CancellationToken _) =>
                role == requested);

        return new FinancialAuthorization(
            new TestUserContext(UserId),
            new TestOrganizationContext(OrganizationId),
            memberships.Object);
    }

    [Theory]
    [InlineData(FinancialCapability.ReceivePayments)]
    [InlineData(FinancialCapability.AllocatePayments)]
    [InlineData(FinancialCapability.VoidPayments)]
    [InlineData(FinancialCapability.IssueInvoices)]
    public async Task Owner_can_perform_every_financial_capability(
        FinancialCapability capability)
    {
        var authorization = CreateAuthorization(OrganizationRole.Owner, out _);

        await authorization.EnsureCanAsync(capability);
    }

    [Theory]
    [InlineData(FinancialCapability.ReceivePayments)]
    [InlineData(FinancialCapability.AllocatePayments)]
    [InlineData(FinancialCapability.IssueInvoices)]
    public async Task Finance_role_can_receive_allocate_and_issue_invoices(
        FinancialCapability capability)
    {
        var authorization = CreateAuthorization(OrganizationRole.Finance, out _);

        await authorization.EnsureCanAsync(capability);
    }

    [Fact]
    public async Task Finance_role_cannot_void_payments()
    {
        var authorization = CreateAuthorization(OrganizationRole.Finance, out _);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            authorization.EnsureCanAsync(FinancialCapability.VoidPayments));
    }

    [Theory]
    [InlineData(FinancialCapability.ReceivePayments)]
    [InlineData(FinancialCapability.AllocatePayments)]
    [InlineData(FinancialCapability.VoidPayments)]
    [InlineData(FinancialCapability.IssueInvoices)]
    public async Task Member_cannot_perform_financial_write_capabilities(
        FinancialCapability capability)
    {
        var authorization = CreateAuthorization(OrganizationRole.Member, out _);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            authorization.EnsureCanAsync(capability));
    }

    [Fact]
    public async Task Nonmember_is_denied_for_selected_organization()
    {
        var authorization = CreateAuthorization(null, out var memberships);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            authorization.EnsureCanAsync(FinancialCapability.ReceivePayments));

        memberships.Verify(x => x.HasRoleAsync(
            UserId,
            OrganizationId,
            OrganizationRole.Owner,
            It.IsAny<CancellationToken>()), Times.Once);
        memberships.Verify(x => x.HasRoleAsync(
            UserId,
            OrganizationId,
            OrganizationRole.Finance,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Unknown_capability_is_denied_without_role_lookup()
    {
        var authorization = CreateAuthorization(OrganizationRole.Owner, out var memberships);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            authorization.EnsureCanAsync((FinancialCapability)999));

        memberships.VerifyNoOtherCalls();
    }
}
