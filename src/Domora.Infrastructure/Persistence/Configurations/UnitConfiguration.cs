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
        builder.HasKey(x => x.Id);

        builder
            .Property(x => x.PropertyId)
            .IsRequired();

        builder
            .Property(x => x.Number)
            .HasConversion(
                number => number.Value,
                value => UnitNumber.Create(value)
            );

        builder
            .HasIndex(x => new
            {
               x.PropertyId,
               x.Number 
            })
            .HasDatabaseName("UX_Units_ProprtyId_Number")
            .IsUnique();

        builder
            .Property(x => x.Type)
            .HasConversion<string>();

        builder
            .Property(x => x.Status)
            .HasConversion<string>();        

        builder
            .HasOne<Property>()
            .WithMany()
            .HasForeignKey(x => x.PropertyId)
            .OnDelete(DeleteBehavior.Restrict); 

        builder.Property<uint>("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsRowVersion();   

        builder
            .Property(x => x.Version)
            .HasColumnType("uuid")
            .ValueGeneratedNever()
            .IsRequired();
    }
}