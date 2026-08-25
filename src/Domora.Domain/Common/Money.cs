using Domora.Domain.Common.Exceptions;

namespace Domora.Domain.Common;

public sealed class Money : IEquatable<Money>
{
    public decimal Amount { get; }

    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        if (amount < 0)
            throw new DomainValidationException("Amount cannot be negative.");

        if (string.IsNullOrWhiteSpace(currency))
            throw new DomainValidationException("Currency is required.");

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