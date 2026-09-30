using Microsoft.AspNetCore.Mvc.Testing;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Domora.Application.Common.Authentication;
using Microsoft.Extensions.Options;
using Domora.Infrastructure.Authentication;


namespace Domora.API.Tests.Authentication;

public sealed class AuthenticationConfigurationTests
{
    public static class TestJwtConfiguration
    {
        public const string Issuer = "Domora.Test";
        public const string Audience = "Domora.Test.Api";
        public const string SigningKey =
            "domora-test-signing-key-must-be-at-least-32-characters-long";
        public const string SigningKeyId = "domora-test-key-v1";
        public const int ExpirationMinutes = 60;
    }

    public sealed class DomoraApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration(
                (_, configuration) =>
                {
                    var testConfiguration = new Dictionary<string, string?>
                    {
                        ["Jwt:Issuer"] = TestJwtConfiguration.Issuer,
                        ["Jwt:Audience"] = TestJwtConfiguration.Audience,
                        ["Jwt:SigningKey"] = TestJwtConfiguration.SigningKey,
                        ["Jwt:SigningKeyId"] = TestJwtConfiguration.SigningKeyId,
                        ["Jwt:ExpirationMinutes"] = TestJwtConfiguration.ExpirationMinutes.ToString()
                    };

                    configuration.AddInMemoryCollection(testConfiguration);
                }
            );
        }
    }

    [Fact]
    public async Task Valid_Jwt_should_authenticate_request_and_expose_user_context()
    {
        // Arrange
        var userId = Guid.NewGuid();

        using var factory = new DomoraApiFactory();

        using var client = factory.CreateClient();

        using var scope = factory.Services.CreateScope();

        var tokenGenerator = scope.ServiceProvider.GetRequiredService<IAccessTokenGenerator>();

        var token = tokenGenerator.Generate(userId);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            token
        );

        // Act
        var response = await client.GetAsync("/auth/me");

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode
        );

        var returnedUserId = await response.Content.ReadFromJsonAsync<Guid>();

        Assert.Equal(
            userId,
            returnedUserId
        );
    }   

    [Fact]
    public async Task Invalid_jwt_should_return_unauthorized()
    {
        // Arrange
        await using var factory = new WebApplicationFactory<Program>();

        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue(
                "Bearer",
                "this-is-not-a-valid-jwt"
            );

        // Act
        var response = await client.GetAsync("/auth/me");

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode
        );
    }

    [Fact] public async Task Missing_jwt_should_return_unauthorized() 
    {    
        // Arrange 
        await using var factory = new WebApplicationFactory<Program>(); 
        using var client = factory.CreateClient(); 
         
        // Act 
        var response = await client.GetAsync("/auth/me"); 
         
        // Assert 
        Assert.Equal( 
            HttpStatusCode.Unauthorized, 
            response.StatusCode 
        ); 
    }
}