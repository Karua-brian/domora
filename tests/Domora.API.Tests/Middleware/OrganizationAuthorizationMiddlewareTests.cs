using System.Net;
using System.Text.Json;
using Domora.API.Tests.Infrastructure;
using Domora.Domain.Organizations.Enums;
using Domora.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Domora.API.Tests.Middleware;

public sealed class OrganizationAuthorizationMiddlewareTests
    : IClassFixture<DomoraWebApplicationFactory>
{
    private readonly DomoraWebApplicationFactory _factory;

    public OrganizationAuthorizationMiddlewareTests(
        DomoraWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<(Guid UserId, Guid OrganizationId)> CreateUserWithOrganizationAsync()
    {
        using var scope =
            _factory.Services.CreateScope();

        var context =
            scope.ServiceProvider
                .GetRequiredService<DomoraDbContext>();

        var user =
            await TestData.CreateUserAsync(context);

        var organization =
            await TestData.CreateOrganizationAsync(context);

        await TestData.AddMembershipAsync(
            context,
            user.Id,
            organization.Id
        );

        return (
            user.Id,
            organization.Id
        );
    }

    [Fact]
    public async Task Authenticated_request_without_organization_header_should_continue()
    {
        // Arrange
        var user =
            await CreateUserWithOrganizationAsync();

        using var client =
            AuthenticatedClientFactory.Create(
                _factory,
                user.UserId
            );

        // Act
        var response =
            await client.GetAsync(
                "/test/organization-context"
            );

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode
        );

        var body =
            await response.Content.ReadAsStringAsync();

        Assert.Equal(
            Guid.Empty.ToString(),
            body.Trim('"')
        );
    }

    [Fact]
    public async Task Malformed_organization_header_should_return_bad_request()
    {
        // Arrange
        var user =
            await CreateUserWithOrganizationAsync();

        using var client =
            AuthenticatedClientFactory.Create(
                _factory,
                user.UserId
            );

        client.DefaultRequestHeaders.Add(
            "X-Organization-Id",
            "not-a-guid"
        );

        // Act
        var response =
            await client.GetAsync(
                "/test/organization-context"
            );

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode
        );
    }

    [Fact]
    public async Task Empty_organization_id_should_return_bad_request()
    {
        // Arrange
        var user =
            await CreateUserWithOrganizationAsync();

        using var client =
            AuthenticatedClientFactory.Create(
                _factory,
                user.UserId
            );

        client.DefaultRequestHeaders.Add(
            "X-Organization-Id",
            Guid.Empty.ToString()
        );

        // Act
        var response =
            await client.GetAsync(
                "/test/organization-context"
            );

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode
        );
    }

    [Fact]
    public async Task User_with_membership_should_establish_organization_context()
    {
        // Arrange
        var user =
            await CreateUserWithOrganizationAsync();

        using var client =
            AuthenticatedClientFactory.Create(
                _factory,
                user.UserId
            );

        client.DefaultRequestHeaders.Add(
            "X-Organization-Id",
            user.OrganizationId.ToString()
        );

        // Act
        var response =
            await client.GetAsync(
                "/test/organization-context"
            );

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode
        );

        var body =
            await response.Content.ReadAsStringAsync();

        var organizationId =
            JsonSerializer.Deserialize<Guid>(body);

        Assert.Equal(
            user.OrganizationId,
            organizationId
        );
    }

    [Fact]
    public async Task User_without_membership_should_receive_forbidden()
    {
        // Arrange
        var user =
            await CreateUserWithOrganizationAsync();

        using var scope =
            _factory.Services.CreateScope();

        var context =
            scope.ServiceProvider
                .GetRequiredService<DomoraDbContext>();

        var otherOrganization =
            await TestData.CreateOrganizationAsync(
                context
            );

        using var client =
            AuthenticatedClientFactory.Create(
                _factory,
                user.UserId
            );

        client.DefaultRequestHeaders.Add(
            "X-Organization-Id",
            otherOrganization.Id.ToString()
        );

        // Act
        var response =
            await client.GetAsync(
                "/test/organization-context"
            );

        // Assert
        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode
        );
    }

    [Fact]
    public async Task Anonymous_request_should_continue_without_organization_context()
    {
        // Arrange
        using var client =
            _factory.CreateClient();

        // Act
        var response =
            await client.GetAsync(
                "/test/organization-context"
            );

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode
        );

        var body =
            await response.Content.ReadAsStringAsync();

        Assert.Equal(
            Guid.Empty.ToString(),
            body.Trim('"')
        );
    }

    [Fact]
    public async Task Organization_context_should_not_leak_between_requests()
    {
        // Arrange
        var first =
            await CreateUserWithOrganizationAsync();

        var second =
            await CreateUserWithOrganizationAsync();

        using var firstClient =
            AuthenticatedClientFactory.Create(
                _factory,
                first.UserId
            );

        firstClient.DefaultRequestHeaders.Add(
            "X-Organization-Id",
            first.OrganizationId.ToString()
        );

        using var secondClient =
            AuthenticatedClientFactory.Create(
                _factory,
                second.UserId
            );

        // Act
        var firstResponse =
            await firstClient.GetAsync(
                "/test/organization-context"
            );

        var secondResponse =
            await secondClient.GetAsync(
                "/test/organization-context"
            );

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            firstResponse.StatusCode
        );

        Assert.Equal(
            HttpStatusCode.OK,
            secondResponse.StatusCode
        );

        var firstBody =
            await firstResponse.Content.ReadAsStringAsync();

        var secondBody =
            await secondResponse.Content.ReadAsStringAsync();

        Assert.Equal(
            first.OrganizationId.ToString(),
            firstBody.Trim('"')
        );

        Assert.Equal(
            Guid.Empty.ToString(),
            secondBody.Trim('"')
        );
    }
}