using Domora.Domain.Properties;

namespace Domora.Infrastructure.Persistence.Repositories;

public sealed class PropertyRepository : IPropertyRepository
{
    private readonly DomoraDbContext _dbContext;

    public PropertyRepository(DomoraDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Property property, CancellationToken cancellationToken)
    {
        await _dbContext.Properties.AddAsync(property, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}