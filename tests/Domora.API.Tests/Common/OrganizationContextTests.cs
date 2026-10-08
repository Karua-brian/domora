using Domora.API.Common;
using Microsoft.AspNetCore.Http;

namespace Domora.API.Tests.Common;

public sealed class OrganizationContextTests
{
    [Fact]
    public void OrganizationId_should_return_set_organization()
    {
        // Arrange
        var organizationId = Guid.NewGuid();

        var httpContext = new DefaultHttpContext();

        var accessor = new HttpContextAccessor
        {
            HttpContext = httpContext
        };

        var context = new OrganizationContext(accessor);

        // Act
        context.Set(organizationId);

        // Assert
        Assert.Equal(
            organizationId,
            context.OrganizationId
        );
    }

    [Fact]
    public void OrganizationId_should_return_empty_when_context_is_missing()
    {
        // Arrange
        var accessor = new HttpContextAccessor();

        var context = new OrganizationContext(accessor);

        // Act
        var organizationId = context.OrganizationId;

        // Assert
        Assert.Equal(
            Guid.Empty,
            organizationId
        );
    }

    [Fact]
    public void OrganizationId_should_return_empty_when_no_organization_has_been_set()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();

        var accessor = new HttpContextAccessor
        {
            HttpContext = httpContext
        };

        var context = new OrganizationContext(accessor);

        // Act
        var organizationId = context.OrganizationId;

        // Assert
        Assert.Equal(
            Guid.Empty,
            organizationId
        );
    }

    [Fact]
    public void Set_should_reject_empty_organization_id()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();

        var accessor = new HttpContextAccessor
        {
            HttpContext = httpContext
        };

        var context = new OrganizationContext(accessor);

        // Act
        var exception = Assert.Throws<ArgumentException>(
            () => context.Set(Guid.Empty)
        );

        // Assert
        Assert.Equal(
            "organizationId",
            exception.ParamName
        );
    }

    [Fact]
    public void Set_should_fail_when_http_context_is_unavailable()
    {
        // Arrange
        var accessor = new HttpContextAccessor();

        var context = new OrganizationContext(accessor);

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => context.Set(Guid.NewGuid())
        );

        // Assert
        Assert.Equal(
            "Organization context is unavailable.",
            exception.Message
        );
    }

    [Fact]
    public void Set_should_replace_existing_organization()
    {
        // Arrange
        var firstOrganizationId = Guid.NewGuid();
        var secondOrganizationId = Guid.NewGuid();

        var httpContext = new DefaultHttpContext();

        var accessor = new HttpContextAccessor
        {
            HttpContext = httpContext
        };

        var context = new OrganizationContext(accessor);

        // Act
        context.Set(firstOrganizationId);
        context.Set(secondOrganizationId);

        // Assert
        Assert.Equal(
            secondOrganizationId,
            context.OrganizationId
        );
    }
}