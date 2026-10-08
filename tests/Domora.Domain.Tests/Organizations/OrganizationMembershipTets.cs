using Domora.Domain.Common.Exceptions;
using Domora.Domain.Organizations;
using Domora.Domain.Organizations.Enums;

namespace Domora.Domain.Tests.Organizations;

public sealed class OrganizationMembershipTests
{
    [Fact]
    public void Create_should_reject_invalid_role()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();

        var invalidRole = (OrganizationRole)999;

        // Act
        var exception = Assert.Throws<DomainValidationException>(
            () => OrganizationMembership.Create(
                userId,
                organizationId,
                invalidRole
            )
        );

        // Assert
        Assert.Equal(
            "Organization role is invalid.",
            exception.Message
        );

    }
}