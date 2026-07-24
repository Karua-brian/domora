namespace Domora.Domain.Properties.ValueObjects;

public sealed class PropertyName
{
    public string Value { get; }

    public PropertyName(string value) 
    {       
        Value = value.Trim();
    }

    public static PropertyName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Property name is required.", nameof(value));

        return new PropertyName(value); 
    }
}