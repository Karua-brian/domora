using Domora.Domain.Common;
using Domora.Domain.Finance;
using Domora.Domain.Finance.Enums;
using Microsoft.EntityFrameworkCore;

namespace Domora.Infrastructure.Persistence.Repositories;

public sealed class PaymentAllocationRepository : IPaymentAllocationRepository
{
    private readonly DomoraDbContext _dbContext;

    public PaymentAllocationRepository(
        DomoraDbContext dbContext
    )
    {
        _dbContext = dbContext;
    }
    public async Task AddAsync(
        PaymentAllocation paymentAllocation,
        CancellationToken cancellationToken
    )
    {
        await _dbContext.PaymentAllocations.AddAsync(
            paymentAllocation,
            cancellationToken
        );
    }

    public async Task<PaymentAllocationReversal?> GetReversalByAllocationIdAsync(
        Guid organizationId, Guid allocationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PaymentAllocationReversals.SingleOrDefaultAsync(
            x => x.OrganizationId == organizationId && x.PaymentAllocationId == allocationId,
            cancellationToken);
    }

    public async Task AddReversalAsync(PaymentAllocationReversal reversal, CancellationToken cancellationToken = default)
    {
        await _dbContext.PaymentAllocationReversals.AddAsync(reversal, cancellationToken);
    }

    public async Task<PaymentAllocation?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken
    )
    {
        return await _dbContext.PaymentAllocations
            .FindAsync(
                new object[] { id },
                cancellationToken
            );
    }

    public async Task<PaymentAllocation?> GetByIdForOrganizationAsync(
        Guid id, Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PaymentAllocations.SingleOrDefaultAsync(
            x => x.Id == id && x.OrganizationId == organizationId, cancellationToken);
    }

    public async Task<Money> GetAllocatedAmountForPaymentAsync(
        Guid paymentId,
        string currency,
        CancellationToken cancellationToken
    )
    {
        var allocatedPaymentAmount = await _dbContext.PaymentAllocations
            .AsNoTracking()
            .Where(x => x.PaymentId == paymentId)
            .SumAsync(x => x.AllocateAmount.Amount, cancellationToken);

        var reversedAmount = await _dbContext.PaymentAllocationReversals.AsNoTracking()
            .Where(x => _dbContext.PaymentAllocations.Any(a => a.OrganizationId == x.OrganizationId
                && a.Id == x.PaymentAllocationId && a.PaymentId == paymentId))
            .SumAsync(x => (decimal?)x.Amount.Amount, cancellationToken) ?? 0m;

        return new Money(allocatedPaymentAmount - reversedAmount, currency);
    }

    public async Task<Money> GetAllocatedAmountForInvoiceAsync(
        Guid invoiceId,
        string currency,
        CancellationToken cancellationToken = default,
        Guid? excludedPaymentId = null
    )
    {
        var allocatedInvoiceAmount = await (
            from allocation in _dbContext.PaymentAllocations.AsNoTracking()
            join payment in _dbContext.Payments.AsNoTracking()
                on new { allocation.OrganizationId, allocation.PaymentId }
                equals new { payment.OrganizationId, PaymentId = payment.Id }
            where allocation.InvoiceId == invoiceId
                && payment.Status == PaymentStatus.Active
                && (!excludedPaymentId.HasValue || allocation.PaymentId != excludedPaymentId.Value)
            select allocation.AllocateAmount.Amount
        ).SumAsync(cancellationToken);

        var reversedAmount = await (
            from reversal in _dbContext.PaymentAllocationReversals.AsNoTracking()
            join allocation in _dbContext.PaymentAllocations.AsNoTracking()
                on new { reversal.OrganizationId, Id = reversal.PaymentAllocationId }
                equals new { allocation.OrganizationId, allocation.Id }
            join payment in _dbContext.Payments.AsNoTracking()
                on new { allocation.OrganizationId, allocation.PaymentId }
                equals new { payment.OrganizationId, PaymentId = payment.Id }
            where allocation.InvoiceId == invoiceId && payment.Status == PaymentStatus.Active
                && (!excludedPaymentId.HasValue || allocation.PaymentId != excludedPaymentId.Value)
            select reversal.Amount.Amount
        ).SumAsync(cancellationToken);

        return new Money(allocatedInvoiceAmount - reversedAmount, currency);
    }

    public async Task<IReadOnlyCollection<PaymentAllocation>> GetAllocationsByPaymentIdAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default
    )
    {
        return await _dbContext.PaymentAllocations
            .Where(pa => pa.PaymentId == paymentId)
            .ToListAsync(cancellationToken);
    }

}
