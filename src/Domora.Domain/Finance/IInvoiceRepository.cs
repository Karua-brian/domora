using Domora.Domain.Leasing;

namespace Domora.Domain.Finance;

public interface IInvoiceRepository
{
    Task AddAsync(
        Invoice invoice,
        CancellationToken cancellationToken = default
    );

    Task<Invoice?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyCollection<Invoice>> GetUnpaidInvoicesByLeaseIdAsync(
        Guid leaseId,
        CancellationToken cancellationToken = default
    );
    Task UpdateAsync(
        Invoice invoice,
        CancellationToken cancellationToken
    );  
}