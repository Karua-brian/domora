namespace Domora.Domain.Organizations.ValueObjects;

public sealed class OrganizationName
{
    public string Value { get; }

    public OrganizationName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Organization name is required.", nameof(value));

        Value = value.Trim();   
    }
}