using Domora.Domain.Finance;
using Microsoft.EntityFrameworkCore;

namespace Domora.Infrastructure.Persistence.Repositories;

public sealed class InvoiceVoidRepository(DomoraDbContext dbContext) : IInvoiceVoidRepository
{
    public Task<InvoiceVoid?> GetByInvoiceIdAsync(Guid organizationId, Guid invoiceId,
        CancellationToken cancellationToken = default) => dbContext.InvoiceVoids.SingleOrDefaultAsync(
        x => x.OrganizationId == organizationId && x.InvoiceId == invoiceId, cancellationToken);

    public Task<InvoiceVoid?> GetByReplacementInvoiceIdAsync(Guid organizationId, Guid replacementInvoiceId,
        CancellationToken cancellationToken = default) => dbContext.InvoiceVoids.SingleOrDefaultAsync(
        x => x.OrganizationId == organizationId && x.ReplacementInvoiceId == replacementInvoiceId, cancellationToken);

    public async Task AddAsync(InvoiceVoid invoiceVoid, CancellationToken cancellationToken = default) =>
        await dbContext.InvoiceVoids.AddAsync(invoiceVoid, cancellationToken);
}
