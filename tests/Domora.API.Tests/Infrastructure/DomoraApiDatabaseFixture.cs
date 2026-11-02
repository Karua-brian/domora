using Domora.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Domora.API.Tests.Infrastructure;

public sealed class DomoraApiDatabaseFixture : IAsyncLifetime
{
    private const int MigrationLockNamespace = 0x444F4D4F;
    private const int MigrationLockKey = 0x52415445;

    public async Task InitializeAsync()
    {
        var connectionString = Environment.GetEnvironmentVariable("DomoraTest");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "DomoraTest connection environment variable is not configured."
            );

        var connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString);
        if (!string.Equals(connectionStringBuilder.Database, "domora_test", StringComparison.Ordinal)
            || !string.Equals(connectionStringBuilder.Host, "localhost", StringComparison.Ordinal)
            || connectionStringBuilder.Port != 5432
            || !string.Equals(connectionStringBuilder.Username, "domora_app", StringComparison.Ordinal))
            throw new InvalidOperationException(
                "API test database initialization may only use domora_app on localhost:5432/domora_test."
            );

        // Keep this session open for the entire migration operation. A session advisory lock
        // coordinates separate test processes as well as classes in this xUnit collection.
        await using var lockConnection = new NpgsqlConnection(connectionString);
        await lockConnection.OpenAsync();
        await using (var acquireLock = new NpgsqlCommand(
            "SELECT pg_advisory_lock(@namespace, @key)", lockConnection))
        {
            acquireLock.Parameters.AddWithValue("namespace", MigrationLockNamespace);
            acquireLock.Parameters.AddWithValue("key", MigrationLockKey);
            await acquireLock.ExecuteNonQueryAsync();
        }

        try
        {
            var options = new DbContextOptionsBuilder<DomoraDbContext>()
                .UseNpgsql(connectionString)
                .Options;

            await using var db = new DomoraDbContext(options);
            await db.Database.MigrateAsync();
        }
        finally
        {
            // Explicit unlock documents the lock boundary. Disposing the connection also
            // releases the session lock if this command fails.
            await using var releaseLock = new NpgsqlCommand(
                "SELECT pg_advisory_unlock(@namespace, @key)", lockConnection);
            releaseLock.Parameters.AddWithValue("namespace", MigrationLockNamespace);
            releaseLock.Parameters.AddWithValue("key", MigrationLockKey);
            await releaseLock.ExecuteNonQueryAsync();
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;
}

[CollectionDefinition(DomoraApiDatabaseCollection.Name)]
public sealed class DomoraApiDatabaseCollection : ICollectionFixture<DomoraApiDatabaseFixture>
{
    public const string Name = "Domora API database";
}
