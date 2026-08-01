using Domora.Domain.Finance;

namespace Domora.Infrastructure.Persistence.Repositories;

public sealed class PaymentRepository : IPaymentRepository
{
    private readonly DomoraDbContext _dbContext;

    public PaymentRepository(
        DomoraDbContext dbContext
    )
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(
        Payment payment,
        CancellationToken cancellationToken
    )
    {
        await _dbContext.Payments.AddAsync(
            payment,
            cancellationToken
        );
    }

    public async Task<Payment?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken
    )
    {
        return await _dbContext.Payments
            .FindAsync(
                new object[] { id },
                cancellationToken
            );
    }

}