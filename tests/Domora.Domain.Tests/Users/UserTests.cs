using Domora.Domain.Users;

namespace Domora.Domain.Tests.Users;

public sealed class UserTests
{
    [Fact]
    public void Register_should_create_user_with_normalized_email()
    {
        // Arrange
        var email = "  TEST@EXAMPLE.COM  ";
        var passwordHash = "hashed-password";

        // Act
        var user = User.Register(
            email,
            passwordHash
        );

        // Assert
        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal(
            "test@example.com",
            user.Email
        );

        Assert.Equal(
            passwordHash,
            user.PasswordHash
        );
    }

    [Fact]
    public void Register_should_reject_missing_email()
    {
        // Arrange
        var passwordHash = "hashed-password";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(
            () => User.Register(
                "  ",
                passwordHash
            )
        );

        Assert.Equal(
            "email",
            exception.ParamName
        );
    }

    [Fact]
    public void Register_should_reject_missing_password_hash()
    {
        // Arrange
        var email = "user@example.com";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(
            () => User.Register(
                email,
                " "
            )
        );

        Assert.Equal(
            "passwordHash",
            exception.ParamName
        );
    }
}