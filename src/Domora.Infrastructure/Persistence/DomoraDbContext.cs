using Domora.Domain.Organizations;
using Domora.Domain.Properties;
using Domora.Domain.Units;
using Microsoft.EntityFrameworkCore;

namespace Domora.Infrastructure.Persistence;

public sealed class DomoraDbContext : DbContext
{
    public DomoraDbContext(DbContextOptions<DomoraDbContext> options) : base(options)
    {
    }

    public DbSet<Organization> Organizations => Set<Organization>();

    public DbSet<Property> Properties => Set<Property>();

    public DbSet<Unit> Units => Set<Unit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DomoraDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}