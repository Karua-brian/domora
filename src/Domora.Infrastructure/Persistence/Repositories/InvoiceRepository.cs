using Domora.Domain.Finance;

namespace Domora.Infrastructure.Persistence.Repositories;

public sealed class InvoiceRepository : IInvoiceRepository
{
    private readonly DomoraDbContext _dbContext;

    public InvoiceRepository(DomoraDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(
        Invoice invoice, 
        CancellationToken cancellationToken
    )
    {
        await _dbContext.Invoices.AddAsync(
            invoice,
            cancellationToken
        );
        
        await _dbContext.SaveChangesAsync(cancellationToken);

    }

    public async Task<Invoice?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken
    )
    {
        return await _dbContext.Invoices
            .FindAsync(
                new object[] { id },
                cancellationToken
            );
    }

    public async Task UpdateAsync(
        Invoice invoice,
        CancellationToken cancellationToken
    )
    {
        _dbContext.Invoices.Update(invoice);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}