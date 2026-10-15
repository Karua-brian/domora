namespace Domora.Domain.Finance;

public interface IPaymentRepository
{
    Task AddAsync(
        Payment payment, 
        CancellationToken cancellationToken = default
    );

    Task<Payment?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    );

    Task<Payment?> GetByReferenceAsync(
        Guid organizationId,
        string reference,
        CancellationToken cancellationToken = default
    );
}