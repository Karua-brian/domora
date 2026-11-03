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

    Task<PaymentAllocation?> GetByIdForOrganizationAsync(
        Guid id, Guid organizationId, CancellationToken cancellationToken = default);

    Task<Money> GetAllocatedAmountForPaymentAsync(
        Guid paymentId,
        string currency,
        CancellationToken cancellationToken = default
    );

    Task<Money> GetAllocatedAmountForInvoiceAsync(
        Guid invoiceId,
        string currency,
        CancellationToken cancellationToken = default,
        Guid? excludedPaymentId = null
    );

    Task<IReadOnlyCollection<PaymentAllocation>> GetAllocationsByPaymentIdAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default
    );

    Task<PaymentAllocationReversal?> GetReversalByAllocationIdAsync(
        Guid organizationId, Guid allocationId, CancellationToken cancellationToken = default);

    Task AddReversalAsync(PaymentAllocationReversal reversal, CancellationToken cancellationToken = default);
}
