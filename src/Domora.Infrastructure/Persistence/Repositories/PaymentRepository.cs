using Domora.Domain.Finance;
using Microsoft.EntityFrameworkCore;

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

    public async Task<Payment?> GetByReferenceAsync(
        Guid organizationId,
        string reference,
        CancellationToken cancellationToken
    )
    {
        return await _dbContext.Payments
            .AsNoTracking()
            .SingleOrDefaultAsync(
                p =>
                    p.OrganizationId == organizationId &&
                    p.Reference == reference,
                cancellationToken
            );
    }

}