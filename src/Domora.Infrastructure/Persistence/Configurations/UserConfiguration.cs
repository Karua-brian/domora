using Domora.Domain.Users;
using Domora.Domain.Users.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domora.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .HasConversion(
                email => email.Value,
                value => UserEmail.Create(value)
            )
            .IsRequired()
            .HasMaxLength(320);

        builder.Property(u => u.PasswordHash)
            .HasConversion(
                passwordHash => passwordHash.Value,
                value => UserPassword.Create(value)
            )
            .IsRequired()
            .HasMaxLength(500);

        builder.HasIndex(u => u.Email)
            .IsUnique();        
    }
}