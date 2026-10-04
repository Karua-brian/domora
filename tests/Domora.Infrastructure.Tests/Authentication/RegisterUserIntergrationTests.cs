using Domora.Application.Common.Context;
using Domora.Application.Users.Commands.RegisterUser;
using Domora.Domain.Common.Exceptions;
using Domora.Domain.Users;
using Domora.Domain.Users.ValueObjects;
using Domora.Infrastructure.Authentication;
using Domora.Infrastructure.Persistence;
using Domora.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Domora.Infrastructure.Tests.Authentication;

public sealed class RegisterUserIntergrationTests
{
    private readonly DbContextOptions<DomoraDbContext> _options;

    private readonly string _connectionString;

    public RegisterUserIntergrationTests()
    {
        _connectionString = 
            Environment.GetEnvironmentVariable("DomoraTest")
            ?? throw new InvalidOperationException(
                "DomoraTest connection is not configured"
            );

        _options = new DbContextOptionsBuilder<DomoraDbContext>()
            .UseNpgsql(_connectionString)
            .Options; 
    }

    private sealed class TestOrganizationContext : IOrganizationContext
    {
        public Guid OrganizationId { get; } = Guid.Empty;
    }

    [Fact]
    public async Task Registering_User_should_hash_password_and_persist_user()
    {
        // Arrange
        var email = $"register-{Guid.NewGuid():N}@example.com";
        var password = $"Hashed-password";
        
        var passwordHasher = new PasswordHasher();

        await using var context = new DomoraDbContext(_options);

        var handler = new RegisterUserHandler(
            new UserRepository(context),
            passwordHasher,
            new UnitOfWork(
                context,
                new TestOrganizationContext()
            )
        );

        var command = new RegisterUserCommand(
            email,
            password
        );

        // Act
        var result = await handler.Handle(
            command,
            CancellationToken.None
        );

        // Assert
        Assert.NotEqual(
            Guid.Empty,
            result.UserId
        );

        await using var verificationContext = 
            new DomoraDbContext(_options);

        var persistedUser = await verificationContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                user => user.Email == UserEmail.Create(email),
                CancellationToken.None
            );

        Assert.NotNull(persistedUser);
        Assert.Equal(
            result.UserId,
            persistedUser.Id
        );

        Assert.True(
            passwordHasher.Verify(
                password,
                persistedUser.PasswordHash.Value
            )
        );

    }

    [Fact]
    public async Task Registering_same_email_twice_should_fail_and_persist_only_one_user()
    {
        // Arrange
        var email = $"register-{Guid.NewGuid():N}@example.com";

        await using var firstContext = new DomoraDbContext(_options);

        var firstHandler = new RegisterUserHandler(
            new UserRepository(firstContext),
            new PasswordHasher(),
            new UnitOfWork(
                firstContext,
                new TestOrganizationContext()
            )
        );

        var command = new RegisterUserCommand(
            email,
            "StrongPass123!"
        );

        // Act
        var firstResult = await firstHandler.Handle(
            command,
            CancellationToken.None
        );

        // The first registration must succeed.
        Assert.NotEqual(
            Guid.Empty,
            firstResult.UserId
        );

        // Registering the same email again using a NEW DbContext.
        await using var secondContext = new DomoraDbContext(_options);

        var secondHandler = new RegisterUserHandler(
            new UserRepository(secondContext),
            new PasswordHasher(),
            new UnitOfWork(
                secondContext,
                new TestOrganizationContext()
            )
        );

        // Assert
        await Assert.ThrowsAsync<ResourceConflictException>(
            () => secondHandler.Handle(
                command,
                CancellationToken.None
            )
        );

        // Verify against PostgreSQL using a completely
        // independent Context
        await using var verificationContext = 
            new DomoraDbContext(_options);

        var persistedUsers = await verificationContext.Users
            .AsNoTracking()
            .Where(user => user.Email == UserEmail.Create(email))
            .ToListAsync();

        Assert.Single(persistedUsers);
        Assert.Equal(
            firstResult.UserId,
            persistedUsers[0].Id
        );
    }
}