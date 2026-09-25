using System.Security.Claims;
using Domora.API.Common;
using Microsoft.AspNetCore.Http;

namespace Domora.API.Tests.Common;

public sealed class UserContextTests
{
    [Fact]
    public void User_id_should_be_read_from_authenticated_claims()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var httpContext = new DefaultHttpContext();

        httpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity(
                new []
                {
                    new Claim(
                        ClaimTypes.NameIdentifier,
                        userId.ToString()
                    )
                },
                authenticationType: "Test"
            )
        );

        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = httpContext
        };

        var userContext = new UserContext(httpContextAccessor);

        // Act
        var result = userContext.UserId;

        // Assert
        Assert.Equal(userId, result);
    }

    [Fact]
    public void User_id_should_fail_when_claim_is_missing()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();

        httpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity(
                authenticationType: "Test"
            )
        );

        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = httpContext
        };

        var userContext = new UserContext(httpContextAccessor);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => userContext.UserId
        );

        Assert.Equal(
            "Authenticated user context is unavailable", 
            exception.Message
        );
    }

    [Fact]
    public void User_id_should_fail_when_claim_is_not_a_guid()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();

        httpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity(
                new []
                {
                    new Claim(
                        ClaimTypes.NameIdentifier,
                        "not-a-guid"
                    )
                },
                authenticationType: "Test"
            )
        );

        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = httpContext
        };

        var userContext = new UserContext(httpContextAccessor);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => userContext.UserId
        );

        Assert.Equal(
            "Authenticated user context is unavailable",
            exception.Message
        );
    }
}