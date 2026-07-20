namespace Domora.Domain.Finance;

public class Payment
{
    public Guid Id { get; }

    public Guid PayerId { get; }

    public Money Amount { get; }

    public DateOnly ReceivedOn { get; }

    public Payment(Guid id, Guid payerId, Money amount, DateOnly receivedOn)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Payment ID is required.", nameof(id));

        if (payerId == Guid.Empty)
            throw new ArgumentException("Payer ID is required.", nameof(payerId));

        if (amount <= 0)
            throw new ArgumentException("Payment amount must be greater than zero.", nameof(amount));

        Id = id;
        PayerId = payerId;
        Amount = amount;
        ReceivedOn = receivedOn;
    }
}