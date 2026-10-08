using Domora.Domain.Common;
using Domora.Domain.Finance;
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
        CancellationToken cancellationToken = default
    )
    {
        var allocatedInvoiceAmount = await _dbContext.PaymentAllocations
            .AsNoTracking()
            .Where(x => x.InvoiceId == invoiceId)
            .SumAsync(x => x.AllocateAmount.Amount, cancellationToken);

        return new Money(allocatedInvoiceAmount, currency);
    }
}