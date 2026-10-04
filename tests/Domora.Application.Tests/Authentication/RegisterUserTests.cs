using Domora.Application.Common.Authentication;
using Domora.Application.Common.Persistence;
using Domora.Application.Users.Commands.RegisterUser;
using Domora.Domain.Common.Exceptions;
using Domora.Domain.Users;
using Domora.Infrastructure.Persistence;

namespace Domora.Application.Tests.Authentication;

public sealed class RegisterUserTests
{
    private sealed class FakeUserRepository : IUserRepository
    {
        public User? User { get; private set; }

        public Task AddAsync(
            User user,
            CancellationToken cancellationToken
        )
        {
            User = user;
            return Task.CompletedTask;
        }
        public Task<User?> FindByEmailAsync(
            string email,
            CancellationToken cancellationToken
        )
        {
            return Task.FromResult(User);
        }
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string RecievedPassword { get; private set; } = string.Empty;
        public bool VerifyResult { get; init; }

        public string Hash(string password)
        {
            RecievedPassword = password;

            return $"hashed:{password}";
        }

        public bool Verify(
            string password,
            string passwordHash
        )
        {
            return VerifyResult;
        }

    }

    private sealed class FakeAccessTokenGenerator : IAccessTokenGenerator
    {
        public string GeneratedToken { get; init; } = "test-token";

        public Guid ReceivedUserId { get; private set; }

        public bool WasCalled { get; private set; }

        public string Generate(Guid userId)
        {
            ReceivedUserId = userId;
            WasCalled = true;

            return GeneratedToken;
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public bool SaveChangesWasCalled { get; private set; }
        public Task<ITransaction> BeginTransactionAsync(
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult<ITransaction>(new FakeTransaction());
        }

        public Task SaveChangesAsync(
            CancellationToken cancellationToken = default
        )
        {
            SaveChangesWasCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeTransaction : ITransaction
    {
        public Task CommitAsync(
            CancellationToken cancellationToken = default
        )
        {
            return Task.CompletedTask;
        }

        public Task RollbackAsync(
            CancellationToken cancellationToken = default
        )
        {
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task Register_should_hash_password_and_persist_user()
    {
        // Arrange
        var users = new FakeUserRepository();

        var passwordHasher = new FakePasswordHasher();

        var unitOfWork = new FakeUnitOfWork();

        var handler = new RegisterUserHandler(
            users,
            passwordHasher,
            unitOfWork
        );

        // Act
        var response = await handler.Handle(
            new RegisterUserCommand(
                "  USER@COMMAND.COM",
                "Secret-password"
            ),
            CancellationToken.None
        );

        // Assert
        Assert.NotEqual(
            Guid.Empty,
            response.UserId
        );

        Assert.Equal(
            "user@command.com",
            response.Email
        );

        Assert.Equal(
            "Secret-password",
            passwordHasher.RecievedPassword
        );

        Assert.NotNull(
            users.User
        );

        Assert.Equal(
            "user@command.com",
            users.User!.Email.Value
        );

        Assert.Equal(
            "hashed:Secret-password",
            users.User.PasswordHash.Value
        );

        Assert.True(
            unitOfWork.SaveChangesWasCalled
        );

    }

    [Fact]
    public async Task Register_should_throw_exception_for_invalid_email()
    {
        // Arrange
        var users = new FakeUserRepository();

        var passwordHasher = new FakePasswordHasher();

        var unitOfWork = new FakeUnitOfWork();

        var handler = new RegisterUserHandler(
            users,
            passwordHasher,
            unitOfWork
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<DomainValidationException>(
            async () => await handler.Handle(
                new RegisterUserCommand(
                    "invalid-email",
                    "Secret-password"
                ),
                CancellationToken.None
            )
        );

        Assert.Equal(
            "User email must contain an '@' symbol.",
            exception.Message
        );

        // Verify that nothing was sent to the database layer due to the crash
        Assert.Null(users.User);;
        Assert.False(unitOfWork.SaveChangesWasCalled);
    }

    [Fact]
    public async Task Register_should_throw_exception_for_invalid_password()
    {
        // Arrange
        var users = new FakeUserRepository();

        var passwordHasher = new FakePasswordHasher();

        var unitOfWork = new FakeUnitOfWork();

        var handler = new RegisterUserHandler(
            users,
            passwordHasher,
            unitOfWork
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<DomainValidationException>(
            async () => await handler.Handle(
                new RegisterUserCommand(
                    "user@example.com",
                    "123"
                ),
                CancellationToken.None
            )
        );

        Assert.Equal(
            "User password must be at least 8 characters long.", 
            exception.Message
        );

        // Verify isolation: process stopped immediately before updating storage or calling the hasher
        Assert.Null(users.User);
        Assert.False(unitOfWork.SaveChangesWasCalled);
        Assert.Equal(string.Empty, passwordHasher.RecievedPassword); 
    }
}