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

    public Money GetOutstandingBalance(Money allocatedAmount)
    {
        return new Money(
            Amount.Amount - allocatedAmount.Amount,
            Amount.Currency
        );
    }
}