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
}