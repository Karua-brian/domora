using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Domora.API.Tests.Infrastructure;
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
    public async Task Authenticated_user_should_register_organization_without_organization_context()
    {
        // Arrange
        Guid userId;

        using (var scope = _factory.Services.CreateScope())
        {
            var context =
                scope.ServiceProvider
                    .GetRequiredService<DomoraDbContext>();

            var user =
                await TestData.CreateUserAsync(context);

            userId = user.Id;
        }

        using var client =
            AuthenticatedClientFactory.Create(
                _factory,
                userId
            );

        var request = new
        {
            name = $"Registration Test Org {Guid.NewGuid():N}"
        };

        // Act
        var response =
            await client.PostAsJsonAsync(
                "/api/organizations",
                request
            );

        // Assert
        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode
        );

        var result =
            await response.Content
                .ReadFromJsonAsync<RegisterOrganizationResponse>();

        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(request.name, result.Name);

        // Verify the owner membership was persisted.
        using var verificationScope =
            _factory.Services.CreateScope();

        var verificationContext =
            verificationScope.ServiceProvider
                .GetRequiredService<DomoraDbContext>();

        var membership =
            await verificationContext
                .OrganizationMemberships
                .SingleOrDefaultAsync(
                    membership =>
                        membership.OrganizationId == result.Id &&
                        membership.UserId == userId
                );

        Assert.NotNull(membership);
        Assert.Equal(userId, membership.UserId);
        Assert.Equal(
            result.Id,
            membership.OrganizationId
        );
        Assert.Equal(
            OrganizationRole.Owner,
            membership.Role
        );
    }

    [Fact]
    public async Task Anonymous_user_should_not_register_organization()
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
