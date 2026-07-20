namespace Domora.Domain.Common;

public sealed class Money : IEquatable<Money>
{
    public decimal Amount { get; }

    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative.", nameof(amount));

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency is required.", nameof(currency));

        Amount = amount;
        Currency = currency.Trim().ToUpper();    
    }

    public bool Equals(Money? other) // 
    {
        if (other is null)
            return false;

        return Amount == other.Amount &&
               Currency == other.Currency;
    }

    public override bool Equals(object? obj) 
        => Equals(obj as Money);

    public override int GetHashCode() 
        => HashCode.Combine(Amount, Currency);
}