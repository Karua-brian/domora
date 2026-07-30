namespace Domora.Domain.Finance;

using Domora.Domain.Common;

public class PaymentAllocation
{
    public Guid Id { get; }

    public Guid PaymentId { get; }

    public Guid InvoiceId { get; }

    public Money AllocateAmount { get; }


    private PaymentAllocation()
    {
        AllocateAmount = null;
    }

    private PaymentAllocation(
        Guid id, 
        Guid paymentId, 
        Guid invoiceId, 
        Money allocateAmount)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Payment allocation ID is required.", nameof(id));

        if (paymentId == Guid.Empty)
            throw new ArgumentException("Payment ID is required.", nameof(paymentId));

        if (invoiceId == Guid.Empty)
            throw new ArgumentException("Invoice ID is required.", nameof(invoiceId));

        Id = id;
        PaymentId = paymentId;
        InvoiceId = invoiceId;
        AllocateAmount = allocateAmount;
    }


    public static PaymentAllocation Allocate(
        Guid paymentId,
        Guid invoiceId,
        Money allocateAmount
    )
    {
        return new PaymentAllocation(
            Guid.NewGuid(),
            paymentId,
            invoiceId,
            allocateAmount
        );
    }

}