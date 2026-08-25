namespace Domora.Domain.Units.ValueObjects;
using Domora.Domain.Common.Exceptions;


public sealed class UnitNumber
{
    public string Value { get;}

    public UnitNumber(string value)
    {
        Value = value.Trim();
    }

    public static UnitNumber Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainValidationException("Unit number is required."); 

        return new UnitNumber(value);  
    }
}