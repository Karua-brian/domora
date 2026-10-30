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

        return new Money(allocatedPaymentAmount, currency);
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

        return new Money(allocatedInvoiceAmount, currency);
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
