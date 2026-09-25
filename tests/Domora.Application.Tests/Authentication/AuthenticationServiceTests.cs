using Domora.Application.Common.Authentication;
using Domora.Application.Common.Persistence;
using Domora.Domain.Users;

namespace Domora.Application.Tests.Authentication;

public sealed class AuthenticationServiceTests
{
    private sealed class FakeUserRepository : IUserRepository
    {
        public User? User { get; init; }

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
        public bool VerifyResult { get; init; }

        public string Hash(string password)
        {
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

    [Fact]
    public async Task Authenticate_when_valid_email_and_valid_password_should_return_user()
    {
        // Arrange
        var email = "user@example.com";
        var password = "correct-password";

        var user = User.Register(
            email,
            "hashed-password"
        );

        var users = new FakeUserRepository
        {
            User = user
        };

        var passwordHasher = new FakePasswordHasher
        {
            VerifyResult = true
        };

        var tokenGenerator = new FakeAccessTokenGenerator();

        var authentication = new AuthenticationService(
            users,
            passwordHasher,
            tokenGenerator
            
        );

        // Act  
        var result = await authentication.AuthenticateAsync(
            email,
            password,
            CancellationToken.None
        );

        // Assert
        Assert.NotNull(result);

        Assert.Equal(
            user.Id,
            result.UserId
        );

        Assert.Equal(
            "test-token",
            result.AccessToken     
        );

        Assert.Equal(
            user.Id,
            tokenGenerator.ReceivedUserId
        );
    }

    [Fact]
    public async Task Authenticate_when_password_is_wrong_should_return_null()
    {
        // Arrange
        var user = User.Register(
            "user@example.com",
            "hashed_password"
        );

        var users = new FakeUserRepository
        {
            User = user
        };

        var passwordHasher = new FakePasswordHasher
        {
            VerifyResult = false
        };

        var tokenGenerator = new FakeAccessTokenGenerator();

        var authentication = new AuthenticationService(
            users,
            passwordHasher,
            tokenGenerator
        );

        // Act
        var result = await authentication.AuthenticateAsync(
            "user@example.com",
            "wrong-password",
            CancellationToken.None
        );

        // Assert
        Assert.Null(result);

        Assert.False(tokenGenerator.WasCalled);
    }

    [Fact]
    public async Task Authenticate_when_user_does_not_exist_should_return_null()
    {
        // Arrange
        var users = new FakeUserRepository
        {
            User = null
        };

        var passwordHasher = new FakePasswordHasher
        {
            VerifyResult = true
        };

        var tokenGenerator = new FakeAccessTokenGenerator();

        var authentication = new AuthenticationService(
            users,
            passwordHasher,
            tokenGenerator
        );

        // Act
        var result = await authentication.AuthenticateAsync(
            "unkown@example.com",
            "passwordHasher",
            CancellationToken.None
        );

        // Assert
        Assert.Null(result);

        Assert.False(tokenGenerator.WasCalled);
    }

    [Fact]
    public async Task Authenticate_when_credentials_are_missing_should_return_null()
    {
        // Arrange 
        var users = new FakeUserRepository
        {
            User = null
        };

        var passwordHasher = new FakePasswordHasher
        {
            VerifyResult = true
        };

        var tokenGenerator = new FakeAccessTokenGenerator();

        var authentication = new AuthenticationService(
            users,
            passwordHasher,
            tokenGenerator
        );

        // Act
        var result = await authentication.AuthenticateAsync(
            "",
            "",
            CancellationToken.None
        );

        // Assert
        Assert.Null(result);

        Assert.False(tokenGenerator.WasCalled);
    }
}