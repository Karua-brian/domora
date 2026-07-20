namespace Domora.Domain.Units.ValueObjects;

public sealed class UnitNumber
{
    public string Value { get;}

    public UnitNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Unit number is required.", nameof(value));

        Value = value.Trim();
    }

    public override string ToString() => Value;
}