using Domora.Application.Finance.Queries.FinancialVisibility;
using Domora.Domain.Finance.Enums;
using Domora.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Domora.Infrastructure.Persistence.Repositories;

public sealed class FinancialVisibilityReadRepository(DomoraDbContext dbContext)
    : IFinancialVisibilityReadRepository
{
    public async Task<IReadOnlyList<InvoiceBalanceReadModel>> GetInvoicesAsync(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var rows = await (
            from invoice in dbContext.Invoices.AsNoTracking()
            where invoice.OrganizationId == organizationId
            join lease in dbContext.Leases.AsNoTracking() on invoice.LeaseId equals lease.Id
            join unit in dbContext.Units.AsNoTracking() on lease.UnitId equals unit.Id
            join property in dbContext.Properties.AsNoTracking() on unit.PropertyId equals property.Id
            where property.OrganizationId == organizationId
            select new
            {
                InvoiceId = invoice.Id, invoice.LeaseId, lease.TenantId, UnitId = unit.Id,
                unit.PropertyId, invoice.Amount.Amount, invoice.Amount.Currency,
                invoice.DueDate, invoice.Status,
                Allocated = (dbContext.PaymentAllocations.AsNoTracking()
                    .Where(a => a.OrganizationId == organizationId && a.InvoiceId == invoice.Id)
                    .Where(a => dbContext.Payments.Any(p => p.OrganizationId == organizationId &&
                        p.Id == a.PaymentId && p.Status == PaymentStatus.Active))
                    .Sum(a => (decimal?)a.AllocateAmount.Amount) ?? 0m)
                    - (dbContext.PaymentAllocationReversals.AsNoTracking()
                        .Where(r => r.OrganizationId == organizationId &&
                            dbContext.PaymentAllocations.Any(a => a.OrganizationId == organizationId &&
                                a.Id == r.PaymentAllocationId && a.InvoiceId == invoice.Id) &&
                            dbContext.Payments.Any(p => p.OrganizationId == organizationId &&
                                p.Id == dbContext.PaymentAllocations.Where(a => a.OrganizationId == organizationId &&
                                    a.Id == r.PaymentAllocationId).Select(a => a.PaymentId).FirstOrDefault() &&
                                p.Status == PaymentStatus.Active))
                        .Sum(r => (decimal?)r.Amount.Amount) ?? 0m)
            }).ToListAsync(cancellationToken);

        var invoiceIds = rows.Select(x => x.InvoiceId).ToArray();
        var cancellations = await dbContext.InvoiceVoids.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && invoiceIds.Contains(x.InvoiceId))
            .ToDictionaryAsync(x => x.InvoiceId, cancellationToken);

        return rows.Select(row => new InvoiceBalanceReadModel(
            row.InvoiceId, row.LeaseId, row.TenantId, row.UnitId, row.PropertyId,
            row.Amount, row.Currency, row.DueDate, row.Status,
            row.Status == InvoiceStatus.Voided ? 0m : Math.Max(0m, row.Allocated),
            row.Status == InvoiceStatus.Voided ? 0m : Math.Max(0m, row.Amount - row.Allocated),
            cancellations.GetValueOrDefault(row.InvoiceId)?.Reason,
            cancellations.GetValueOrDefault(row.InvoiceId)?.ActorUserId,
            cancellations.GetValueOrDefault(row.InvoiceId)?.VoidedAtUtc,
            cancellations.GetValueOrDefault(row.InvoiceId)?.ReplacementInvoiceId)).ToList();
    }
}
