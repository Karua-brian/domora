namespace Domora.Domain.Finance;

using Domora.Domain.Common.Exceptions;
using Domora.Domain.Common;

public class Payment
{
    public Guid Id { get; }

    public Money TotalAmount { get; }

    public DateTimeOffset PaidAt { get; }

    public string Reference { get; }

    public Guid Version { get; private set; }

    private Payment()
    {
        TotalAmount = null;
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
            throw new DomainValidationException("Payment ID is required.");

        Id = id;
        TotalAmount = amount;
        PaidAt = paidAt;
        Reference = refrence;
        Version = Guid.NewGuid();
    }

    public static Payment Receive(
        Money amount,
        string reference
    )
    {
        return new Payment(
            Guid.NewGuid(),
            amount,
            DateTimeOffset.UtcNow,
            reference
        );
    }

    public Money GetRemainingBalance(
        Money allocatedToPayment
    )
    {
        return new Money(
            TotalAmount.Amount - allocatedToPayment.Amount,
            TotalAmount.Currency
        );
    }

    public void EnsureCanAllocate(
        Money allocateAmount,
        Money allocatedToPaymentSoFar
    )
    {
        var remaining = GetRemainingBalance(allocatedToPaymentSoFar);

        if (remaining.Amount < allocateAmount.Amount);
            throw new DomainValidationException(
                "Payment has insufficient remaining balance to satisfy this allocation request."
            );
    }
}