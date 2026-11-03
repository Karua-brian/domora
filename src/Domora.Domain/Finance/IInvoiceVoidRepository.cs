namespace Domora.Domain.Finance;

public interface IInvoiceVoidRepository
{
    Task<InvoiceVoid?> GetByInvoiceIdAsync(Guid organizationId, Guid invoiceId,
        CancellationToken cancellationToken = default);

    Task<InvoiceVoid?> GetByReplacementInvoiceIdAsync(Guid organizationId, Guid replacementInvoiceId,
        CancellationToken cancellationToken = default);

    Task AddAsync(InvoiceVoid invoiceVoid, CancellationToken cancellationToken = default);
}
