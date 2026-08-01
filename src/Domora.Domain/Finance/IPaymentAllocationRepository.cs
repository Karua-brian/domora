using Domora.Domain.Common;

namespace Domora.Domain.Finance;

public interface IPaymentAllocationRepository
{
    Task AddAsync(
        PaymentAllocation paymentAllocation,
        CancellationToken cancellationToken = default
    );

    Task<PaymentAllocation?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    );

    Task<Money> GetAllocatedAmountForPaymentAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default
    );

    Task<Money> GetAllocatedAmountForInvoiceAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default
    );
}