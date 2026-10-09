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
        string currency,
        CancellationToken cancellationToken = default
    );

    Task<Money> GetAllocatedAmountForInvoiceAsync(
        Guid invoiceId,
        string currency,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyCollection<PaymentAllocation>> GetAllocationsByPaymentIdAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default
    );
    void Remove(PaymentAllocation paymentAllocation);
}