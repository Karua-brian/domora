using Domora.Domain.Finance;

namespace Domora.Infrastructure.Persistence.Repositories;

public sealed class PaymentVoidRepository : IPaymentVoidRepository
{
    private readonly DomoraDbContext _dbContext;

    public PaymentVoidRepository(DomoraDbContext dbContext) => _dbContext = dbContext;

    public async Task AddAsync(PaymentVoid paymentVoid, CancellationToken cancellationToken = default)
    {
        await _dbContext.PaymentVoids.AddAsync(paymentVoid, cancellationToken);
    }
}
