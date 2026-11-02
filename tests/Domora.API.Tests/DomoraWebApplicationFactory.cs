using Domora.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Domora.API.Tests;

public static class TestJwtConfiguration
{
    public const string Issuer = "domora";
    public const string Audience = "domora.API";
    public const string SigningKey = "domora-test-signing-key-must-be-at-least-32-characters-long!";
    public const string SigningKeyId = "domora-test-key-v1";
    public const string ExpirationMinutes = "60";
}

public class DomoraWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        // Override your appsettings configuration keys so your token generator 
        // and validation middleware use the exact same test keys!
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            var testConfiguration = new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = TestJwtConfiguration.Issuer,
                ["Jwt:Audience"] = TestJwtConfiguration.Audience,
                ["Jwt:SigningKey"] = TestJwtConfiguration.SigningKey,
                ["Jwt:ExpirationMinutes"] = TestJwtConfiguration.ExpirationMinutes
            };

            configuration.AddInMemoryCollection(testConfiguration);
        });

        builder.ConfigureServices(services =>
        {
            // Locate and strip out your default production Npgsql DbContext options registration
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<DomoraDbContext>));

            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            // Replace the application DbContext registration with the verified test database.
            var connectionString = Environment.GetEnvironmentVariable("DomoraTest");
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException(
                    "DomoraTest connection environment variable is not configured."
                );

            if (!string.Equals(
                    new NpgsqlConnectionStringBuilder(connectionString).Database,
                    "domora_test",
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "API integration tests may only run against the dedicated domora_test database."
                );

            services.AddDbContext<DomoraDbContext>(options =>
            {
                options.UseNpgsql(connectionString);
            });
        });
    }
}
