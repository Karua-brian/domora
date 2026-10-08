using Domora.Application.Common.Context;
using Domora.Application.Common.Persistence;
using Domora.Application.Organizations.Commands.RegisterOrganization;
using Domora.Domain.Organizations;
using Domora.Domain.Organizations.Enums;
using Domora.Domain.Users;
using Moq;

namespace Domora.Application.Tests.Organizations.Commands.RegisterOrganization;

public sealed class RegisterOrganizationHandlerTests
{
    [Fact]
    public async Task Handle_should_create_organization_and_owner_membership()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var organizationRepository =
            new Mock<IOrganizationRepository>();

        var membershipRepository =
            new Mock<IOrganizationMembershipRepository>();

        var unitOfWork =
            new Mock<IUnitOfWork>();

        var userContext =
            new Mock<IUserContext>();

        userContext
            .Setup(context => context.UserId)
            .Returns(userId);

        var handler = new RegisterOrganizationHandler(
            organizationRepository.Object,
            membershipRepository.Object,
            userContext.Object,
            unitOfWork.Object
        );

        var command = new RegisterOrganizationCommand(
            "Muthama Cribs"
        );

        Organization? capturedOrganization = null;
        OrganizationMembership? capturedMembership = null;

        organizationRepository
            .Setup(repository =>
                repository.AddAsync(
                    It.IsAny<Organization>(),
                    It.IsAny<CancellationToken>()
                ))
            .Callback<Organization, CancellationToken>(
                (organization, _) =>
                    capturedOrganization = organization
            );

        membershipRepository
            .Setup(repository =>
                repository.AddAsync(
                    It.IsAny<OrganizationMembership>(),
                    It.IsAny<CancellationToken>()
                ))
            .Callback<OrganizationMembership, CancellationToken>(
                (membership, _) =>
                    capturedMembership = membership
            );

        // Act
        var response = await handler.Handle(
            command,
            CancellationToken.None
        );

        // Assert
        Assert.NotNull(capturedOrganization);
        Assert.NotNull(capturedMembership);

        Assert.Equal(
            capturedOrganization!.Id,
            response.Id
        );

        Assert.Equal(
            "Muthama Cribs",
            response.Name
        );

        Assert.Equal(
            userId,
            capturedMembership!.UserId
        );

        Assert.Equal(
            capturedOrganization.Id,
            capturedMembership.OrganizationId
        );

        Assert.Equal(
            OrganizationRole.Owner,
            capturedMembership.Role
        );

        unitOfWork.Verify(
            unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }
}