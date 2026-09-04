using Domora.Domain.Properties;
using Microsoft.EntityFrameworkCore;

namespace Domora.Infrastructure.Persistence.Repositories;

public sealed class PropertyRepository : IPropertyRepository
{
    private readonly DomoraDbContext _dbContext;

    public PropertyRepository(DomoraDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(
        Property property, 
        CancellationToken cancellationToken
    )
    {
        await _dbContext.Properties.AddAsync(
            property, 
            cancellationToken
        );

    }

    public async Task<Property?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken
    )
    {
        return await _dbContext.Properties
            .SingleOrDefaultAsync(
                p => p.Id == id,
                cancellationToken
            );
    }
}