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
            throw new ArgumentException("Organization name is required.", nameof(value));

        return new OrganizationName(value);
    }
}