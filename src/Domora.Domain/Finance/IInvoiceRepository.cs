using Domora.Domain.Finance.Enums;
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

    Task<bool> HasInvoiceGeneratedForPeriodAsync(
        Guid leaseId,
        InvoiceType type,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default
    );

    Task UpdateAsync(
        Invoice invoice,
        CancellationToken cancellationToken
    );  
}