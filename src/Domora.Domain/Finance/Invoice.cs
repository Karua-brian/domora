namespace Domora.Domain.Finance;

public class Invoice
{
    public Guid Id { get; }

    public Guild LeaseId { get; }

    public decimal Amount { get; }

    public DateOnly IssueDate { get; }

    public DateOnly DueDate { get; }

    public Invoice(Guid id, Guid leaseId, decimal amount, DateOnly issueDate, DateOnly dueDate)
    {
        if (id == Guid.Empty)
            throw new ArguementException("Invoice ID is required.", nameof(id));

        if (leaseId == Guid.Empty)
            throw new ArguementException("Lease ID is required.", nameof(leaseId));

        if (amount <= 0)
            throw new ArguementException("Invoice amount must be greater than zero.", nameof(amount));    

        if (dueDate < issueDate)
            throw new ArguementException("Due date cannot be before issue date.", nameof(dueDate));

        Id = id,
        LeaseId = leaseId,
        Amount = amount,
        IssueDate = issueDate,
        DueDate = dueDate;        
    }
}