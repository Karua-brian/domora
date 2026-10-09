namespace Domora.Domain.Finance;

using Domora.Domain.Common;
using Domora.Domain.Common.Exceptions;
using Domora.Domain.Finance.Enums;

public class Invoice
{
    public Guid Id { get; }

    public Guid LeaseId { get; }

    public Money Amount { get; }

    public DateOnly DueDate { get; }

    public InvoiceType Type { get; }

    public InvoiceStatus Status { get; private set;}

    public Guid Version { get; private set; } //  

    private Invoice()
    {
        Amount = null!;
    }

    private Invoice(
        Guid id, 
        Guid leaseId, 
        Money amount, 
        DateOnly dueDate, 
        InvoiceType type,
        InvoiceStatus status
        )
    {
        if (id == Guid.Empty)
            throw new DomainValidationException("Invoice ID is required.");

        if (leaseId == Guid.Empty)
            throw new DomainValidationException("Lease ID is required.");

        Id = id;
        LeaseId = leaseId;
        Amount = amount;
        DueDate = dueDate;
        Type = type;
        Status = status;
        Version = Guid.NewGuid(); 
    }   

    public static Invoice Create(
        Guid leaseId,
        InvoiceType type,
        Money amount,
        DateOnly dueDate
    )
    {
        return new Invoice(
            Guid.NewGuid(),
            leaseId,
            amount,
            dueDate,
            type,
            InvoiceStatus.Pending        
        );
    }

    public void MarkAsPaid()
    {
        if (Status == InvoiceStatus.Paid)
            throw new ResourceConflictException(
                "Invoice is marked as paid"
            );

        Status = InvoiceStatus.Paid;
        Version = Guid.NewGuid();
    }

    public Money GetOutstandingBalance(
        Money allocatedToInvoice
    )
    {
        return new Money(
            Amount.Amount - allocatedToInvoice.Amount,
            Amount.Currency
        );
    }

    public decimal AllocatePayment(
        Money paymentPool,
        Money previouslyAllocatedAmount
    )
    {
         if (Amount.Currency != paymentPool.Currency)
            throw new DomainValidationException(
                $"Currency mismatch. Cannot allocate {paymentPool.Currency} from a {Amount.Currency} payment."
            );
        
        // Calculate how much money this invoice is still waiting for
        var outstanding = GetOutstandingBalance(
            previouslyAllocatedAmount       
        );

        // If it's already 0 or negative, you cannot add money to it
        if (outstanding.Amount <= 0)
            throw new ResourceConflictException(
                "Invoice has already been fully settled."
            );

        // Take either the full payment pool, or just what is needed to clear the bill
        var actualAppliedAmount = Math.Min(
            paymentPool.Amount, outstanding.Amount
        ); 
        
        // Calculate final processed allocation bounds
        var remainingBalanceAfterAllocation = 
            outstanding.Amount - actualAppliedAmount;

        // Update the invoice status state automatically
        if (remainingBalanceAfterAllocation == 0)
        {
            MarkAsPaid();
        }
        else if (actualAppliedAmount > 0)
        {
            Status = InvoiceStatus.PartiallyPaid;
            Version = Guid.NewGuid();
        }
        
        return actualAppliedAmount;
    }

    public void ReverseAllocation(
        Money allocatedAmountToReturn,
        Money totalAllocatedToInvoiceSoFar
    )
    {
        if (Amount.Currency != allocatedAmountToReturn.Currency)
            throw new DomainValidationException(
                "Currency mismatch during payment allocation reversal."
            );

        var outstanding = GetOutstandingBalance(totalAllocatedToInvoiceSoFar);
        var newOutstandingAmount = outstanding.Amount + allocatedAmountToReturn.Amount;

        if (newOutstandingAmount > Amount.Amount)
            throw new DomainValidationException(
                "Cannot reverse more money than the total baseline invoice value limit."
            );

        if (newOutstandingAmount == Amount.Amount)
        {
            Status = InvoiceStatus.Pending;
        }
        else if (newOutstandingAmount > 0)
        {
            Status = InvoiceStatus.PartiallyPaid;
        }

        Version = Guid.NewGuid();
    }
}