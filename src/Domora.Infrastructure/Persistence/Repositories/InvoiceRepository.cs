using Domora.Domain.Finance;
using Domora.Domain.Finance.Enums;
using Microsoft.EntityFrameworkCore;

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

    public async Task<IReadOnlyCollection<Invoice>> GetUnpaidInvoicesByLeaseIdAsync(
        Guid leaseId,
        CancellationToken cancellationToken
    )
    {
        return await _dbContext.Invoices
            .Where(i => i.LeaseId == leaseId && i.Status != InvoiceStatus.Paid)
            .OrderBy(i => i.DueDate) // Enforces FIFO sorting rules at the database boundary
            .ThenBy(i => i.Type)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        Invoice invoice,
        CancellationToken cancellationToken
    )
    {
        _dbContext.Invoices.Update(invoice);

    }
}