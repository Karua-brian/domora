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

    public async Task<Invoice?> GetByIdForOrganizationAsync(
        Guid id, Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Invoices.SingleOrDefaultAsync(
            x => x.Id == id && x.OrganizationId == organizationId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Invoice>> GetUnpaidInvoicesByLeaseIdAsync(
        Guid leaseId,
        CancellationToken cancellationToken
    )
    {
        return await _dbContext.Invoices
            .Where(i => i.LeaseId == leaseId && i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Voided)
            .OrderBy(i => i.DueDate)
            .ThenBy(i => i.Type == InvoiceType.Rent ? 0
                : i.Type == InvoiceType.Water ? 1
                : i.Type == InvoiceType.Garbage ? 2
                : 3)
            .ThenBy(i => i.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasInvoiceGeneratedForPeriodAsync(
        Guid leaseId,
        InvoiceType type,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken
    )
    {
        return await _dbContext.Invoices
            .AsNoTracking()
            .AnyAsync(i =>
                i.LeaseId == leaseId &&
                i.Type == type &&
                i.DueDate >= startDate &&
                i.DueDate <= endDate,
                cancellationToken
            );
    }


    public async Task UpdateAsync(
        Invoice invoice,
        CancellationToken cancellationToken
    )
    {
        _dbContext.Invoices.Update(invoice);

    }
}
