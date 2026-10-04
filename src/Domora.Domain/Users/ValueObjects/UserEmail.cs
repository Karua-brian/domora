using Domora.Domain.Common.Exceptions;

namespace Domora.Domain.Users.ValueObjects;

public sealed class UserEmail
{
    public string Value { get; }

    private UserEmail(string value)
    {
        Value = value.Trim().ToLowerInvariant();
    }

    public static UserEmail Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainValidationException(
                "User email is required."
            );

        if (!value.Contains('@'))
            throw new DomainValidationException(
                "User email must contain an '@' symbol."
            );

        if (!new[] { ".com", ".net", ".org" }
                .Any(domain => value.EndsWith(domain, System.StringComparison.OrdinalIgnoreCase)))
            throw new DomainValidationException(
                "User email must end with a valid domain (e.g., .com, .net, .org)."
            );

        if (value.Length > 255)
            throw new DomainValidationException(
                "User email must not exceed 255 characters."
            );

        if (value.Length < 5)
            throw new DomainValidationException(
                "User email must be at least 5 characters long."
            );    

        if (value.Contains(".."))
            throw new DomainValidationException(
                "User email must not contain consecutive dots."
            );

        if (value.StartsWith('.') || value.EndsWith('.'))
            throw new DomainValidationException(
                "User email must not start or end with a dot."
            );

        if (value.StartsWith('@') || value.EndsWith('@'))
            throw new DomainValidationException(
                "User email must not start or end with an '@' symbol."
            );

        if (value.IndexOf('@') != value.LastIndexOf('@'))
            throw new DomainValidationException(
                "User email must not contain more than one '@' symbol."
            );

        return new UserEmail(value);
    }
}