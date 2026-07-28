using Domora.Domain.Leasing;

namespace Domora.Infrastructure.Persistence.Repositories;

public sealed class LeaseRepository : ILeaseRepository
{
    private readonly DomoraDbContext _dbContext;

    public LeaseRepository(DomoraDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Lease lease, CancellationToken cancellationToken)
    {
        await _dbContext.Leases.AddAsync(lease, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}