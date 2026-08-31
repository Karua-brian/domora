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

    public InvoiceStatus Status { get; private set;}

    public Guid Version { get; private set; } //  

    private Invoice()
    {
        Amount = null;
    }

    private Invoice(
        Guid id, 
        Guid leaseId, 
        Money amount, 
        DateOnly dueDate, 
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
        Status = status;
        Version = Guid.NewGuid(); 
    }   

    public static Invoice Create(
        Guid leaseId,
        Money amount,
        DateOnly dueDate
    )
    {
        return new Invoice(
            Guid.NewGuid(),
            leaseId,
            amount,
            dueDate,
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

    public Money GetOutstandingBalance(Money allocatedToInvoice)
    {
        return new Money(
            Amount.Amount - allocatedToInvoice.Amount,
            Amount.Currency
        );
    }

    public decimal AllocatePayment(
        Money allocateAmount,
        Money allocatedToInvoiceSoFar
    )
    {
        var outstanding = GetOutstandingBalance(allocatedToInvoiceSoFar);

        if (outstanding.Amount <= 0)
            throw new ResourceConflictException(
                "Invoice has already been fully settled."
            );

        if (outstanding.Amount < allocateAmount.Amount)
            throw new DomainValidationException(
                $"Allocation amount exceeds the invoice outstanding balance"
            ); 
        
        // Calculate final processed allocation bounds
        var allocationAmount = Math.Min(
            allocateAmount.Amount, outstanding.Amount
        );

        var outstandingAfterAllocation = outstanding.Amount - allocationAmount;

        if (outstandingAfterAllocation == 0)
        {
            MarkAsPaid();
        }

        return allocationAmount;
    }
}