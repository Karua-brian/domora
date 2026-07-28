namespace Domora.Domain.Finance;

using Domora.Domain.Common;
using Domora.Domain.Finance.Enums;

public class Invoice
{
    public Guid Id { get; }

    public Guid LeaseId { get; }

    public Money Amount { get; }

    public DateOnly DueDate { get; }

    public InvoiceStatus Status { get; private set;}

    private Invoice()
    {
        Amount = null;
    }

    private Invoice(Guid id, Guid leaseId, Money amount, DateOnly dueDate, InvoiceStatus status)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Invoice ID is required.", nameof(id));

        if (leaseId == Guid.Empty)
            throw new ArgumentException("Lease ID is required.", nameof(leaseId));

        Id = id;
        LeaseId = leaseId;
        Amount = amount;
        DueDate = dueDate;
        Status = status;
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
            InvoiceStatus.Paid);
    }
}