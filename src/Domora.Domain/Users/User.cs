namespace Domora.Domain.Users;

public sealed class User
{
    public Guid Id { get; }

    public string Email { get; }

    public string PasswordHash { get; }

    private User(
        Guid id,
        string email,
        string passwordHash
    )
    {
        Id = id;
        Email = email;
        PasswordHash = passwordHash;
    }

    public static User Register(
        string email,
        string passwordHash
    )
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException(
                "Email is required.", 
                nameof(email)
            );

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException(
                "Password hash is required.",
                nameof(passwordHash)
            );

        return new User(
            Guid.NewGuid(),
            email.Trim().ToLowerInvariant(),
            passwordHash
        );
    }
}