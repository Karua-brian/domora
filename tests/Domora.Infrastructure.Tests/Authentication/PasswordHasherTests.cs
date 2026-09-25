using Domora.Infrastructure.Authentication;

namespace Domora.Infrastructure.Tests.Authentication;

public sealed class PasswordHasherTests
{
    [Fact]
    public void Hash_should_not_return_plaintext_password()
    {
        // Arrange
        var hasher = new PasswordHasher();
        var password = "correct-password";

        // Act
        var hash = hasher.Hash(password);

        // Assert
        Assert.NotEqual(password, hash);
        Assert.False(string.IsNullOrWhiteSpace(hash));

    }

    [Fact]
    public void Verify_should_return_true_for_correct_password()
    {
        // Arrange
        var hasher = new PasswordHasher();
        var password = "correct-password";

        var hash = hasher.Hash(password);

        // Act
        var result = hasher.Verify(
            password,
            hash
        );

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Verify_should_return_false_for_wrong_password()
    {
        // Arrange
        var hasher = new PasswordHasher();
        var password = "correct-pass";

        var hash = hasher.Hash(password);

        // Act
        var result = hasher.Verify(
            "wrong-password",
            hash
        );

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Hash_should_produce_different_hashes_for_same_password()
    {
        // Arrange
        var hasher = new PasswordHasher();
        var password = "correct-pass";

        // Act
        var firstHash = hasher.Hash(password);
        var secondHash = hasher.Hash(password); 

        // Assert
        Assert.NotEqual(
            firstHash,
            secondHash
        ); 
        
        Assert.True(
            hasher.Verify(password, firstHash)
        );

        Assert.True(
            hasher.Verify(password, secondHash)
        );
    }

    [Fact]
    public void Verify_should_return_false_when_password_is_missing()
    {
        // Arrange
        var hasher = new PasswordHasher();
        var hash = hasher.Hash("correct-password");

        // Act
        var result = hasher.Verify(
            "",
            hash
        );

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Verify_should_return_false_when_hash_is_missing()
    {
        // Arrange
        var hasher = new PasswordHasher();

        // Act
        var result = hasher.Verify(
            "correct-password",
            ""
        );

        // Assert
        Assert.False(result);
    }
}