using Domora.Domain.Finance;
using Domora.Domain.Leasing;
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

    public DbSet<Lease> Leases => Set<Lease>();

    public DbSet<Invoice> Invoices => Set<Invoice>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<PaymentAllocation> PaymentAllocations => Set<PaymentAllocation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DomoraDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default
    )
    {
        // foreach (var entry in ChangeTracker.Entries())
        // {
        //     if (entry.State == EntityState.Modified &&
        //         entry.Metadata.FindProperty("Version") is not null)
        //     {
        //         entry.Property("Version").CurrentValue = Guid.NewGuid();
        //     }
        // }

        return await base.SaveChangesAsync(cancellationToken);
    }
}