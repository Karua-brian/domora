
using Domora.Domain.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domora.Infrastructure.Persistence.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasKey(x => x.Id);

        builder
            .OwnsOne(x => x.TotalAmount, money =>
            {
                money.Property(x => x.Amount)
                    .HasColumnName("Amount")
                    .HasPrecision(18, 2)
                    .IsRequired();

                money.Property(x => x.Currency)
                    .HasColumnName("Currency")
                    .HasMaxLength(3)
                    .IsRequired();
            });

        builder
            .OwnsOne(p => p.UnallocatedAmount, money =>
            {
                money.Property(m => m.Amount)
                    .HasColumnName("UnallocatedAmount")
                    .HasPrecision(18, 2);

                money.Property(m => m.Currency)
                    .HasColumnName("Currency")
                    .HasMaxLength(3);
            });

        builder
            .Property(x => x.PaidAt)
            .IsRequired();

        builder
            .Property(x => x.Reference)
            .HasMaxLength(100)
            .IsRequired();

        builder
            .HasIndex(x => x.Reference)
            .IsUnique();

        builder
            .Property(x => x.Version)
            .HasColumnType("uuid")
            .ValueGeneratedNever()
            .IsConcurrencyToken()
            .IsRequired();
    }
}