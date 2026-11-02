using Domora.Application.Common.Context;
using Domora.Application.Common.Exceptions;
using Domora.Application.Common.Persistence;
using Domora.Application.Organizations.Commands;
using Domora.Application.Organizations.Commands.AddOrganizationMember;
using Domora.Application.Organizations.Commands.ChangeOrganizationMemberRole;
using Domora.Application.Organizations.Commands.RemoveOrganizationMember;
using Domora.Domain.Organizations;
using Domora.Domain.Organizations.Enums;
using Moq;

namespace Domora.Application.Tests.Organizations;

public sealed class OrganizationMembershipHandlerAuthorizationTests
{
    private sealed class TestContext(Guid userId, Guid organizationId) : IUserContext, IOrganizationContext
    {
        public Guid UserId { get; } = userId;
        public Guid OrganizationId { get; } = organizationId;
        public void Set(Guid organizationId) => throw new NotSupportedException();
    }

    [Fact]
    public async Task Direct_handler_calls_reject_nonowner_membership_mutations()
    {
        var organizationId = Guid.NewGuid();
        var owner = OrganizationMembership.Create(
            Guid.NewGuid(), organizationId, OrganizationRole.Owner);
        var financeActor = Guid.NewGuid();
        var targetUser = Guid.NewGuid();
        var context = new TestContext(financeActor, organizationId);

        var memberships = new Mock<IOrganizationMembershipRepository>();
        memberships.Setup(x => x.LockOrganizationAsync(
                organizationId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        memberships.Setup(x => x.GetOwnersAsync(
                organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { owner });

        var users = new Mock<IUserRepository>();
        users.Setup(x => x.ExistsAsync(targetUser, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var transaction = new Mock<ITransaction>();
        transaction.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);

        var guard = new OwnershipMutationGuard(context, context, memberships.Object);
        var addHandler = new AddOrganizationMemberHandler(
            guard, memberships.Object, users.Object, unitOfWork.Object);
        var changeHandler = new ChangeOrganizationMemberRoleHandler(
            guard, memberships.Object, unitOfWork.Object);
        var removeHandler = new RemoveOrganizationMemberHandler(
            guard, memberships.Object, unitOfWork.Object);

        await Assert.ThrowsAsync<ForbiddenException>(() => addHandler.Handle(
            new AddOrganizationMemberCommand(targetUser, OrganizationRole.Member),
            CancellationToken.None));
        await Assert.ThrowsAsync<ForbiddenException>(() => changeHandler.Handle(
            new ChangeOrganizationMemberRoleCommand(targetUser, OrganizationRole.Finance),
            CancellationToken.None));
        await Assert.ThrowsAsync<ForbiddenException>(() => removeHandler.Handle(
            new RemoveOrganizationMemberCommand(targetUser),
            CancellationToken.None));

        memberships.Verify(x => x.AddAsync(
            It.IsAny<OrganizationMembership>(), It.IsAny<CancellationToken>()), Times.Never);
        memberships.Verify(x => x.RemoveAsync(
            It.IsAny<OrganizationMembership>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
