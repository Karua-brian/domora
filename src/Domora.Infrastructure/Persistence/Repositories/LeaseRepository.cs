using Domora.Domain.Leasing;
using Domora.Domain.Leasing.Enums;
using Domora.Application.Common.Context;
using Microsoft.EntityFrameworkCore;

namespace Domora.Infrastructure.Persistence.Repositories;

public sealed class LeaseRepository : ILeaseRepository
{
    private readonly DomoraDbContext _dbContext;
    private readonly IOrganizationContext _organizationContext;

    public LeaseRepository(
        DomoraDbContext dbContext,
        IOrganizationContext organizationContext
        )
    {
        _dbContext = dbContext;
        _organizationContext = organizationContext;
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
                l => l.Id == id,
                cancellationToken
                );
    }

    public async Task<IReadOnlyCollection<Lease>> GetActiveLeasesAsync(
        CancellationToken cancellationToken
    )
    {
        var organizationId = _organizationContext.OrganizationId;

        var query =
            from lease in _dbContext.Leases
            join unit in _dbContext.Units
                on lease.UnitId equals unit.Id
            join property in _dbContext.Properties
                on unit.PropertyId equals property.Id
            where lease.Status == LeaseStatus.Active
                && property.OrganizationId == organizationId
            select lease;

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<bool> HasActiveLeaseAsync(
        Guid unitId,
        CancellationToken cancellationToken
    )
    {
        return await _dbContext.Leases
            .AsNoTracking()
            .AnyAsync(
                l => l.UnitId == unitId &&
                l.Status == LeaseStatus.Active
            );

    }

    public async Task UpdateAsync(Lease lease, 
    CancellationToken cancellationToken)
    {
        _dbContext.Leases.Update(lease);
        await Task.CompletedTask;

    }
}