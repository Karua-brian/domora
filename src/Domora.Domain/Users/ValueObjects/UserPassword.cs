using Domora.Domain.Common.Exceptions;

namespace Domora.Domain.Users.ValueObjects;

public sealed class UserPassword
{
    public string Value { get; }

    private UserPassword(string value)
    {
        Value = value;
    }

    public static UserPassword Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainValidationException(
                "User password is required."
            );

        if (value.Length < 6)
            throw new DomainValidationException(
                "User password must be at least 8 characters long."
            );

        if (value.Length > 128)
            throw new DomainValidationException(
                "User password must not exceed 128 characters."
            );

        if (!value.Any(char.IsUpper))
            throw new DomainValidationException(
                "User password must contain at least one uppercase letter."
            );
            
        if (value.Contains(' '))
            throw new DomainValidationException(
                "User password must not contain spaces."
            );

        if (value.Contains('\t'))
            throw new DomainValidationException(
                "User password must not contain tabs."
            );    

        if (!value.Any(char.IsLower))
            throw new DomainValidationException(
                "User password must contain at least one lowercase letter."
            );
        // if (!value.Any(char.IsDigit))
        //     throw new DomainValidationException(
        //         "User password must contain at least one digit."
        //     );

        // if (!value.Any(ch => !char.IsLetterOrDigit(ch)))
        //      throw new DomainValidationException(
        //          "User password must contain at least one special character."
        //      );
        
        return new UserPassword(value);
    }

    public static UserPassword FromHash(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
            throw new DomainValidationException(
                "User password hash is required."
            );

        return new UserPassword(hash);
    }
}