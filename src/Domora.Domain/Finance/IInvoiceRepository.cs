using Domora.Domain.Leasing;

namespace Domora.Domain.Finance;

public interface IInvoiceRepository
{
    Task AddAsync(
        Invoice invoice,
        CancellationToken cancellationToken = default
    );

}