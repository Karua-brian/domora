
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
            .OwnsOne(x => x.Amount, money =>
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