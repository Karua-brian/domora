namespace Domora.Domain.Finance;

using Domora.Domain.Common.Exceptions;
using Domora.Domain.Common;

public class PaymentAllocation
{
    public Guid Id { get; }

    public Guid OrganizationId { get; }

    public Guid PaymentId { get; }

    public Guid InvoiceId { get; }

    public Money AllocateAmount { get; }


    private PaymentAllocation()
    {
        AllocateAmount = null!;
    }

    private PaymentAllocation(
        Guid id, 
        Guid organizationId,
        Guid paymentId, 
        Guid invoiceId, 
        Money allocateAmount)
    {
        if (id == Guid.Empty)
            throw new DomainValidationException(
                "Payment allocation ID is required."
            );

        if (paymentId == Guid.Empty)
            throw new DomainValidationException(
                "Payment ID is required."
            );

        if (invoiceId == Guid.Empty)
            throw new DomainValidationException(
                "Invoice ID is required."
            );
        
        if (allocateAmount.Amount <= 0)
            throw new DomainValidationException(
                "Payment allocation amount must be greater than zero."
            );

        Id = id;
        OrganizationId = organizationId;
        PaymentId = paymentId;
        InvoiceId = invoiceId;
        AllocateAmount = allocateAmount;
    }

    public static PaymentAllocation Allocate(
        Guid organizationId,
        Guid paymentId,
        Guid invoiceId,
        Money allocateAmount
    )
    {
        return new PaymentAllocation(
            Guid.NewGuid(),
            organizationId,
            paymentId,
            invoiceId,
            allocateAmount
        );
    }

}