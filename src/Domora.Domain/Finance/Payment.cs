namespace Domora.Domain.Finance;

using Domora.Domain.Common.Exceptions;
using Domora.Domain.Common;
using Domora.Domain.Finance.Enums;

public class Payment
{
    public Guid Id { get; }
    public Money TotalAmount { get; }
    public Money UnallocatedAmount { get; private set; } // Tracks leftover account credits
    public DateTimeOffset PaidAt { get; }
    public string Reference { get; }
    public PaymentStatus Status { get; private set; }
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
        UnallocatedAmount = new Money(amount.Amount, amount.Currency);
        PaidAt = paidAt;
        Reference = refrence;
        Status = PaymentStatus.Active;
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

    public void Void()
    {
        if (Status == PaymentStatus.Voided)
            throw new DomainValidationException(
                "This payment entry has already been voided."
            );

        Status = PaymentStatus.Voided;
        UnallocatedAmount = new Money(0m, TotalAmount.Currency); // Wipe out available accounr credits
        Version = Guid.NewGuid();
    }

    public void DeductCredit(
        Money amount
    )
    {
        if (Status == PaymentStatus.Voided)
            throw new ResourceConflictException(
                "Cannot modify credit pools on a voided payment."
            );

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