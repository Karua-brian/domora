namespace Domora.Domain.Finance;

using Domora.Domain.Common.Exceptions;
using Domora.Domain.Common;

public class Payment
{
    public Guid Id { get; }
    public Money TotalAmount { get; }
    public Money UnallocatedAmount { get; private set; } // Tracks leftover account credits
    public DateTimeOffset PaidAt { get; }
    public string Reference { get; }
    public Guid Version { get; private set; }
    private Payment()
    {
        TotalAmount = null!;
        UnallocatedAmount = null!;
        Reference = null!;
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
        UnallocatedAmount = amount;
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

    public void DeductCredit(
        Money amount
    )
    {
        if (TotalAmount.Currency != amount.Currency)
            throw new DomainValidationException(
                "Currency mismatch while deducting payment credit."
            ); 

        if (UnallocatedAmount.Amount < amount.Amount)
            throw new DomainValidationException(
                "Cannot deduct more credit than what is available inside this payment."
            );

        UnallocatedAmount = new Money(
            UnallocatedAmount.Amount - amount.Amount,
            TotalAmount.Currency
        );
        Version = Guid.NewGuid();
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
        if (TotalAmount.Currency != allocateAmount.Currency)
            throw new DomainValidationException(
            $"Currency mismatch. Cannot allocate {allocateAmount.Currency} from a {TotalAmount.Currency} payment."
            );

        var remaining = GetRemainingBalance(
            allocatedToPaymentSoFar
        );

        if (remaining.Amount < allocateAmount.Amount)
            throw new DomainValidationException(
                "Payment has insufficient remaining balance to satisfy this allocation request."
            );
    }
}