using Domora.Application.Common.Authorization;
using Domora.Application.Common.Persistence;
using Moq;

namespace Domora.Application.Tests.Authorization;

public sealed class OrganizationAccessTests
{
    [Fact]
    public async Task User_should_be_allowed_when_membership_exists()
    {
        // Arrange
        var mockRepo = new Mock<IOrganizationMembershipRepository>();

        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();

        mockRepo
            .Setup(repo => repo.ExistsAsync(userId, organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var access = new OrganizationAccess(mockRepo.Object);

        // Act
        var result = await access.CanAccessAsync(
            userId,
            organizationId,
            CancellationToken.None
        );

        // Assert
        Assert.True(result);

        mockRepo.Verify(
            repo => repo.ExistsAsync(userId, organizationId, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task User_should_be_denied_when_membership_does_not_exist()
    {
        // Arrange
        var mockRepo = new Mock<IOrganizationMembershipRepository>();

        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();

        mockRepo
            .Setup(repo => repo.ExistsAsync(userId, organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var access = new OrganizationAccess(mockRepo.Object);

        // Act
        var result = await access.CanAccessAsync(
            userId,
            organizationId,
            CancellationToken.None
        );

        // Assert
        Assert.False(result);

        mockRepo.Verify(
            repo => repo.ExistsAsync(userId, organizationId, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }
}