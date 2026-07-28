using Domora.Domain.Leasing;
using Domora.Domain.Units;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domora.Infrastructure.Persistence.Configurations;

public sealed class LeaseConfiguration : IEntityTypeConfiguration<Lease>
{
    public void Configure(EntityTypeBuilder<Lease> builder)
    {
        builder.HasKey(x => x.Id);

        builder
            .Property(x => x.UnitId)
            .IsRequired();

        builder
            .Property(x => x.TenantId)
            .IsRequired();   

        builder
            .Property(x => x.StartDate)
            .IsRequired(); 

        builder
            .HasOne<Unit>()
            .WithMany()
            .HasForeignKey(x => x.UnitId)
            .OnDelete(DeleteBehavior.Restrict);    

        builder
            .OwnsOne(
                x => x.MonthlyRent,
                money =>
                {
                    money.Property(x => x.Amount)
                        .HasColumnName("MonthlyRent")
                        .HasPrecision(18, 2)
                        .IsRequired();

                    money.Property(x => x.Currency)
                        .HasColumnName("MonthlyRentCurrency")
                        .HasMaxLength(3)
                        .IsRequired();
                }
            );
    }
}