using Domora.Application.Common.Authentication;
using Domora.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace Domora.Infrastructure.Authentication;

public sealed class PasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _hasher = new();

    public string Hash(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException(
                "Password is required.",
                nameof(password)
            );
        
        return _hasher.HashPassword(
            null!,
            password
        );
        
    }

    public bool Verify(
        string password,
        string passwordHash
    )
    {
        if (string.IsNullOrWhiteSpace(password))
            return false;

        if (string.IsNullOrWhiteSpace(passwordHash))
            return false;

        var result = _hasher.VerifyHashedPassword(
            null!,
            passwordHash,
            password
        );

        return result == PasswordVerificationResult.Success
            || result == PasswordVerificationResult.SuccessRehashNeeded;
    }
}