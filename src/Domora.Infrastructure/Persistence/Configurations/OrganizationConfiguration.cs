using Domora.Domain.Organizations;
using Domora.Domain.Organizations.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace Domora.Infrastructure.Persistence.Configurations;

public sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.HasKey(x => x.Id);

        builder
            .Property(x => x.Name)
            .HasConversion(
                name => name.Value,
                value => OrganizationName.Create(value)
            )
            .HasMaxLength(100);
    }
}





