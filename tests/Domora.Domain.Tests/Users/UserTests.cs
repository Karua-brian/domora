using Domora.Domain.Common.Exceptions;
using Domora.Domain.Users;
using Domora.Domain.Users.ValueObjects;

namespace Domora.Domain.Tests.Users;

public sealed class UserTests
{
    [Fact]
    public void Register_should_create_user_with_normalized_email()
    {
        // Arrange
        var email = "  TEST@EXAMPLE.COM";
        var passwordHash = "Hashed-password";

        // Act
        var user = User.Register(
            UserEmail.Create(email),
            UserPassword.Create(passwordHash)
        );

        // Assert
        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal(
            "test@example.com",
            user.Email.Value
        );

        Assert.Equal(
            passwordHash,
            user.PasswordHash.Value
        );
    }

    [Fact]
    public void Register_should_reject_missing_email()
    {
        // Arrange
        var passwordHash = "Hashed-password";

        // Act & Assert
        var exception = Assert.Throws<DomainValidationException>(
            () => User.Register(
               UserEmail.Create(" "),
               UserPassword.Create(passwordHash)
            )
        );

        Assert.Equal(
            "User email is required.",
            exception.Message
        );
    }

    [Fact]
    public void Register_should_reject_missing_password_hash()
    {
        // Arrange
        var email = "user@example.com";

        // Act & Assert
        var exception = Assert.Throws<DomainValidationException>(
            () => User.Register(
                UserEmail.Create(email),
               UserPassword.Create(" ")
            )
        );

        Assert.Equal(
            "User password is required.",
            exception.Message
        );
    }
}