
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
            .OwnsOne(p => p.TotalAmount, money =>
            {
                money.Property(m => m.Amount)
                    .HasColumnName("TotalAmount")
                    .HasPrecision(18, 2)
                    .IsRequired();

                money.Property(m => m.Currency)
                    .HasColumnName("TotalCurrency")
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
                    .HasColumnName("UnallocatedCurrency")
                    .HasMaxLength(3);
            });

        builder
            .Property(p => p.Status)
            .HasConversion<string>()
            .IsRequired();

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