using Domora.Domain.Organizations;
using Microsoft.EntityFrameworkCore;

namespace Domora.Infrastructure.Persistence;

public sealed class DomoraDbContext : DbContext
{
    public DomoraDbContext(DbContextOptions<DomoraDbContext> options) : base(options)
    {
    }

    public DbSet<Organization> Organizations => Set<Organization>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DomoraDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}