namespace Domora.Domain.Finance;

using Domora.Domain.Common;

public class Invoice
{
    public Guid Id { get; }

    public Guid LeaseId { get; }

    public Money Amount { get; }

    public DateOnly IssueDate { get; }

    public DateOnly DueDate { get; }

    public Invoice(Guid id, Guid leaseId, Money amount, DateOnly issueDate, DateOnly dueDate)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Invoice ID is required.", nameof(id));

        if (leaseId == Guid.Empty)
            throw new ArgumentException("Lease ID is required.", nameof(leaseId));

        if (amount.Amount <= 0)
            throw new ArgumentException("Invoice amount must be greater than zero.", nameof(amount));    

        if (dueDate < issueDate)
            throw new ArgumentException("Due date cannot be before issue date.", nameof(dueDate));

        Id = id;
        LeaseId = leaseId;
        Amount = amount;
        IssueDate = issueDate;
        DueDate = dueDate;        
    }
}