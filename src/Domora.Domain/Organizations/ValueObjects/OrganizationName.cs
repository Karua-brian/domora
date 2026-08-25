using Domora.Domain.Common.Exceptions;

namespace Domora.Domain.Organizations.ValueObjects;

public sealed class OrganizationName
{
    public string Value { get; }

    private OrganizationName(string value)
    {
        Value = value.Trim();
    }

    public static OrganizationName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainValidationException("Organization name is required.");

        return new OrganizationName(value);
    }
}