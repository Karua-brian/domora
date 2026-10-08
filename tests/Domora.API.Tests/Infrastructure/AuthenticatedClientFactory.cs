using System.Net.Http.Headers;
using Domora.Application.Common.Authentication;
using Domora.Infrastructure.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Domora.API.Tests.Infrastructure;

public static class AuthenticatedClientFactory
{
    public static HttpClient Create(
        DomoraWebApplicationFactory factory,
        Guid userId)
    {
        var client = factory.CreateClient();

        using var scope =
            factory.Services.CreateScope();

        var tokenGenerator =
            scope.ServiceProvider
                .GetRequiredService<IAccessTokenGenerator>();

        var token =
            tokenGenerator.Generate(userId);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token
            );

        return client;
    }
}