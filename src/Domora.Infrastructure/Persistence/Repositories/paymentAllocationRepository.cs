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
        CancellationToken cancellationToken
    )
    {
        var payment = await _dbContext.Payments
            .FindAsync(new object[] { paymentId }, cancellationToken);

        if (payment is null)
            throw new InvalidOperationException("Payment not found.");    

        var allocatedAmount = await _dbContext.PaymentAllocations
            .Where(x => x.PaymentId == paymentId)
            .SumAsync(x => x.AllocateAmount.Amount, cancellationToken);

        return new Money(allocatedAmount, payment.TotalAmount.Currency);
    }

    public async Task<Money> GetAllocatedAmountForInvoiceAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default
    )
    {
        var invoice = await _dbContext.Invoices
            .FindAsync( new object[] { invoiceId }, cancellationToken);

        if (invoice is null)
            throw new InvalidOperationException("Invoice not found.");

        var allocatedAmount = await _dbContext.PaymentAllocations
            .Where(x => x.InvoiceId == invoiceId)
            .SumAsync(x => x.AllocateAmount.Amount, cancellationToken);

        return new Money(allocatedAmount, invoice.Amount.Currency);
    }
}