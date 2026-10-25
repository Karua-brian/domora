using Domora.Domain.Properties;
using Domora.Domain.Units;
using Domora.Domain.Units.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domora.Infrastructure.Persistence.Configurations;

public sealed class UnitConfiguration : IEntityTypeConfiguration<Unit>
{
    public void Configure(EntityTypeBuilder<Unit> builder)
    {
        builder.HasKey(u => u.Id);

        builder
            .Property(u => u.PropertyId)
            .IsRequired();

        builder
            .Property(u => u.Number)
            .HasConversion(
                number => number.Value,
                value => UnitNumber.Create(value)
            );

        builder
            .HasIndex(u => new
            {
               u.PropertyId,
               u.Number 
            })
            .HasDatabaseName("UX_Units_ProprtyId_Number")
            .IsUnique();

        builder
            .Property(u => u.Type)
            .HasConversion<string>();

        builder
            .Property(u => u.Status)
            .HasConversion<string>();        

        builder
            .HasOne<Property>()
            .WithMany()
            .HasForeignKey(u => u.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);    

        builder
            .Property(u => u.Version)
            .HasColumnType("uuid")
            .ValueGeneratedNever()
            .IsRequired()
            .IsConcurrencyToken();
    }
}