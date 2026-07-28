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
}