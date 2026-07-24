using Domora.Domain.Organizations;
using Domora.Domain.Properties;
using Domora.Domain.Properties.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domora.Infrastructure.Persistence.Configurations;

public sealed class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.HasKey(x => x.Id);

        builder
            .Property(x => x.OrganizationId) //
            .IsRequired();

        builder
            .Property(x => x.Name)
            .HasConversion(
                name => name.Value,
                value => PropertyName.Create(value)
            )
            .HasMaxLength(100)
            .IsRequired();
            
        builder
            .HasOne<Organization>()
            .WithMany()
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}