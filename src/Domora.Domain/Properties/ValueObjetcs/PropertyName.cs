namespace Domora.Domain.Properties.ValueObjects;

public sealed class PropertyName
{
    public string Value { get; }

    public PropertyName(string value) 
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Property name is required.", nameof(value));
                
        Value = value.Trim();
    }
}