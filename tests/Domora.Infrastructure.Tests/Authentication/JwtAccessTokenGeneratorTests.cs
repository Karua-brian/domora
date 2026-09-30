using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Domora.Infrastructure.Authentication;
using Microsoft.Extensions.Options;

namespace Domora.Infrastructure.Tests.Authentication;

public sealed class JwtAccessTokenGeneratorTests
{
    private static JwtAccessTokenGenerator CreateGenerator()
    {
        var options = Options.Create(
            new JwtOptions
            {
                Issuer = "domora",
                Audience = "domora-api",
                SigningKey = "this-is-a-development-signing-key-that-is-long-enough",
                ExpirationMinutes = 60
            }
        );

        return new JwtAccessTokenGenerator(options);
    }

    [Fact]
    public void Generate_should_create_token_containing_user_id()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var genrator = CreateGenerator();

        // Act
        var token = genrator.Generate(userId);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(token));

        var handler = new JwtSecurityTokenHandler();

        var jwt = handler.ReadJwtToken(token);

        var userIdClaim = jwt.Claims.Single(
            claim => claim.Type == ClaimTypes.NameIdentifier
        );

        Assert.Equal(
            userId.ToString(), 
            userIdClaim.Value
        );
    }

    [Fact]
    public void Generate_should_set_issuer_and_audience()
    {
        // Arrange
        var genrator = CreateGenerator();

        // Act
        var token = genrator.Generate(Guid.NewGuid());

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        // Assert
        Assert.Equal(
            "domora",
            jwt.Issuer
        );

        Assert.Contains(
            "domora-api",
            jwt.Audiences
        );

    }
}