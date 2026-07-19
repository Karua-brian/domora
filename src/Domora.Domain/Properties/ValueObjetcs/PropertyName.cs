namespace Domora.Domain.Properties.ValueObjects;

public sealed class Property
{
    public PropertyName Value { get; }

    public PropertyName(string value) 
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArguementException("Property name is required.", nameof(value));

        Value = value.Trim();
    }
}