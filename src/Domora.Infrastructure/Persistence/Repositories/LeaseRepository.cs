using Domora.Domain.Leasing;
using Microsoft.EntityFrameworkCore;

namespace Domora.Infrastructure.Persistence.Repositories;

public sealed class LeaseRepository : ILeaseRepository
{
    private readonly DomoraDbContext _dbContext;

    public LeaseRepository(DomoraDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(
        Lease lease, 
        CancellationToken cancellationToken)
    {
        await _dbContext.Leases.AddAsync(lease, cancellationToken);
        
    }

    public async Task<Lease?> GetByIdAsync(
        Guid id, 
        CancellationToken cancellationToken)
    {
        return await _dbContext.Leases
            .SingleOrDefaultAsync(
                x => x.Id == id,
                cancellationToken
                );
    }

    public async Task UpdateAsync(Lease lease, 
    CancellationToken cancellationToken)
    {
        _dbContext.Leases.Update(lease);

    }
}