namespace Domora.Domain.Finance;

using Domora.Domain.Common;

public class PaymentAllocation
{
    public Guid Id { get; }

    public Guid PaymentId { get; }

    public Guid InvoiceId { get; }

    public Money AllocatedAmount { get; }

    public PaymentAllocation(Guid id, Guid paymentId, Guid invoiceId, Money allocatedAmount)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Payment allocation ID is required.", nameof(id));

        if (paymentId == Guid.Empty)
            throw new ArgumentException("Payment ID is required.", nameof(paymentId));

        if (invoiceId == Guid.Empty)
            throw new ArgumentException("Invoice ID is required.", nameof(invoiceId));

        if (allocatedAmount.Amount <= 0)
            throw new ArgumentException("Allocated amount must be greater than zero.", nameof(allocatedAmount));

        Id = id;
        PaymentId = paymentId;
        InvoiceId = invoiceId;
        AllocatedAmount = allocatedAmount;
    }
}