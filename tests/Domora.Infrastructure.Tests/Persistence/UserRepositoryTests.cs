using Domora.Domain.Users;
using Domora.Infrastructure.Persistence;
using Domora.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Domora.Infrastructure.Tests.Persistence;

public sealed class UserRepositoryTests
{
    private readonly string _connectionString;

    public UserRepositoryTests()
    {
        _connectionString = 
            Environment.GetEnvironmentVariable("DomoraTest")
            ?? throw new InvalidOperationException(
                "DomoraTest connection is not configured"
            );
    }

    private DomoraDbContext CreateContext()
    {
        var options = 
            new DbContextOptionsBuilder<DomoraDbContext>()
                .UseNpgsql(_connectionString)
                .Options;

        return new DomoraDbContext(options);
    }

    [Fact]
    public async Task Find_by_email_should_return_existing_user()
    {
        // Arrange
        var email = $"user-{Guid.NewGuid():N}@example.com";

        var user = User.Register(
            email,
            "hashed-password"
        );

        await using (var context = CreateContext())
        {
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();
        }

        await using var readContext = CreateContext();

        var repository = new UserRepository(readContext);

        // Act
        var result = await repository.FindByEmailAsync(
            email,
            CancellationToken.None
        );

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
        Assert.Equal(email, result.Email);
    }
    [Fact]
    public async Task Find_by_email_should_return_null_when_user_does_not_exist()
    {
        // Arrange
        await using var context = CreateContext();

        var repo = new UserRepository(context);

        // Act
        var result = await repo.FindByEmailAsync(
            $"missing-{Guid.NewGuid():N}@example.com",
            CancellationToken.None
        );

        // Assert
        Assert.Null(result);
    }
}