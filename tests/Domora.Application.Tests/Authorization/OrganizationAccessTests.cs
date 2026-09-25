using Domora.Application.Common.Authorization;
using Domora.Application.Common.Persistence;

namespace Domora.Application.Tests.Authorization;

public sealed class OrganizationAccessTests
{
    private sealed class FakeOrganizationMembershipRepo : IOrganizationMembershipRepository
    {
        public bool ExistsResult { get; init; }

        public Guid ReceivedUserId { get; private set;}

        public Guid ReceivedOrganizationId { get; private set; }

        public Task<bool> ExistsAsync(
            Guid userId,
            Guid organizationId,
            CancellationToken cancellationToken
        )
        {
            ReceivedUserId = userId;
            ReceivedOrganizationId = organizationId;

            return Task.FromResult(ExistsResult);
        }    
    }

    [Fact]
    public async Task User_should_be_allowed_when_membership_exists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();

        var memberships = new FakeOrganizationMembershipRepo
        {
            ExistsResult = true
        };

        var access = new OrganizationAccess(memberships);

        // Act
        var result = await access.CanAccessAsync(
            userId,
            organizationId,
            CancellationToken.None
        );

        // Assert
        Assert.True(result);
        Assert.Equal(userId, memberships.ReceivedUserId);
        Assert.Equal(organizationId, memberships.ReceivedOrganizationId);
    }

    [Fact]
    public async Task User_should_be_denied_when_membership_does_not_exist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();

        var memberships = new FakeOrganizationMembershipRepo
        {
            ExistsResult = false
        };

        var access = new OrganizationAccess(memberships);

        // Act
        var result = await access.CanAccessAsync(
            userId,
            organizationId,
            CancellationToken.None
        );

        // Assert
        Assert.False(result);
        Assert.Equal(userId, memberships.ReceivedUserId);
        Assert.Equal(organizationId, memberships.ReceivedOrganizationId);
    }
}