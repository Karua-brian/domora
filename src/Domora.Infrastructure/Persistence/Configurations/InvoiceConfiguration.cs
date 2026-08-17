using Domora.Domain.Finance;
using Domora.Domain.Leasing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domora.Infrastructure.Persistence.Configurations;

public sealed class InvoiceConfigurations : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.HasKey(x => x.Id);

        builder
            .Property(x => x.LeaseId)
            .IsRequired();

        builder
            .OwnsOne(x => x.Amount, 
            money =>
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
            .Property(x => x.DueDate)
            .IsRequired();
        
        builder 
            .Property(x => x.Status)
            .HasConversion<string>();

        builder
            .HasOne<Lease>()
            .WithMany()
            .HasForeignKey(x => x.LeaseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .Property(x => x.Version)
            .HasColumnType("uuid")
            .ValueGeneratedNever()
            .IsConcurrencyToken()
            .IsRequired();
    }
}