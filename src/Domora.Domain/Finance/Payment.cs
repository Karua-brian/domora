namespace Domora.Domain.Finance;

using Domora.Domain.Common;

public class Payment
{
    public Guid Id { get; }

    public Money Amount { get; }

    public DateTimeOffset PaidAt { get; }

    public string Reference { get; }

    public Guid Version { get; private set; }

    private Payment()
    {
        Amount = null;
        Reference = null;
    }

    private Payment(
        Guid id, 
        Money amount, 
        DateTimeOffset paidAt, 
        string refrence
        
        )
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Payment ID is required.", nameof(id));

        Id = id;
        Amount = amount;
        PaidAt = paidAt;
        Reference = refrence;
        Version = Guid.NewGuid();
    }

    public static Payment Receive(
        Money amount,
        DateTimeOffset paidAt,
        string reference
    )
    {
        return new Payment(
            Guid.NewGuid(),
            amount,
            paidAt,
            reference
        );
    }

    public Money GetRemainingBalance(Money allocatedAmount)
    {
        return new Money(
            Amount.Amount - allocatedAmount.Amount,
            Amount.Currency
        );
    }
}