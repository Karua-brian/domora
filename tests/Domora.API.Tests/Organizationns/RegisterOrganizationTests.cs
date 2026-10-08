using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Domora.Domain.Organizations.Enums;
using Domora.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit; // Ensure Xunit is explicitly here for the [Fact] attribute

namespace Domora.API.Tests.Organizations; // 🚀 FIX: Removed the accidental space before the dot

public sealed class RegisterOrganizationTests : IClassFixture<DomoraWebApplicationFactory>
{
    private readonly DomoraWebApplicationFactory _factory;

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
        public RegisterOrganizationTests(
            DomoraWebApplicationFactory factory
        )
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_should_create_organization_and_owner_membership()
    {
        // Arrange
        var client = _factory.CreateClient();

        var email = $"owner-{Guid.NewGuid():N}@example.com";
        const string password = "SecurePassword123!"; 

        // Post to User Registration
        var registerUserResponse = await client.PostAsJsonAsync(
            "auth/register",
            new
            {
                Email = email,
                Password = password
            }
        );
        registerUserResponse.EnsureSuccessStatusCode();

        // Post to Login to acquire token stream
        var loginResponse = await client.PostAsJsonAsync(
            "auth/login",
            new
            {
                Email = email,
                Password = password
            }
        );
        loginResponse.EnsureSuccessStatusCode(); 

        var authentication = await loginResponse
            .Content
            .ReadFromJsonAsync<AuthenticationResponse>();

        Assert.NotNull(authentication);

        // Attach Bearer Token to Request Headers
        client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue(
                "Bearer",
                authentication!.AccessToken
            );

        var organizationName = $"Organization-{Guid.NewGuid():N}";

        // Act: Create the organization entry
        var response = await client.PostAsJsonAsync(
            "api/organizations",
            new
            {
                Name = organizationName
            }
        );

        // Assert
        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode
        );

        var organizationResponse = await response
            .Content
            .ReadFromJsonAsync<RegisterOrganizationResponse>();

        Assert.NotNull(organizationResponse);

        // 4. Verify Database Integrity State using an isolated scoped read
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DomoraDbContext>();

        var organization = await dbContext.Organizations.FindAsync(
            organizationResponse!.Id
        );
        Assert.NotNull(organization);

        var membership = await dbContext.OrganizationMemberships
            .SingleOrDefaultAsync(
                m => m.OrganizationId == organizationResponse.Id
            );

        Assert.NotNull(membership);
        Assert.Equal(
            organizationResponse.Id, 
            membership!.OrganizationId
        );
        Assert.Equal(
            OrganizationRole.Owner, 
            membership.Role
        );
    }

    [Fact]
    public async Task Register_should_require_authentication()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/organizations",
            new
            {
                Name = $"Unauthorized-{Guid.NewGuid():N}"
            }
        );

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode
        );
    }

    private sealed record AuthenticationResponse(
        string AccessToken
    );

    private sealed record RegisterOrganizationResponse(
        Guid Id,
        string Name
    );
}
