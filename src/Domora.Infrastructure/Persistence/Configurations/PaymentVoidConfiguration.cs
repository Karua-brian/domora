using Domora.Domain.Finance;
using Domora.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domora.Infrastructure.Persistence.Configurations;

public sealed class PaymentVoidConfiguration : IEntityTypeConfiguration<PaymentVoid>
{
    public void Configure(EntityTypeBuilder<PaymentVoid> builder)
    {
        builder.ToTable("PaymentVoids");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OrganizationId).IsRequired();
        builder.Property(x => x.PaymentId).IsRequired();
        builder.Property(x => x.ActorUserId).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.VoidedAtUtc).IsRequired();

        builder.HasIndex(x => new { x.OrganizationId, x.PaymentId })
            .IsUnique().HasDatabaseName("UX_PaymentVoids_OrganizationId_PaymentId");

        builder.HasOne<Payment>().WithOne()
            .HasForeignKey<PaymentVoid>(x => new { x.OrganizationId, x.PaymentId })
            .HasPrincipalKey<Payment>(x => new { x.OrganizationId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>().WithMany()
            .HasForeignKey(x => x.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
