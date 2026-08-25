using Domora.Domain.Common.Exceptions;

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
            throw new DomainValidationException("Property name is required.");

        return new PropertyName(value); 
    }
}