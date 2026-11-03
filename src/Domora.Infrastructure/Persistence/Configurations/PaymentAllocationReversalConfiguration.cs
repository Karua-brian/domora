using Domora.Domain.Finance;
using Domora.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domora.Infrastructure.Persistence.Configurations;

public sealed class PaymentAllocationReversalConfiguration : IEntityTypeConfiguration<PaymentAllocationReversal>
{
    public void Configure(EntityTypeBuilder<PaymentAllocationReversal> builder)
    {
        builder.ToTable("PaymentAllocationReversals");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OrganizationId).IsRequired();
        builder.Property(x => x.PaymentAllocationId).IsRequired();
        builder.Property(x => x.ActorUserId).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.ReversedAtUtc).IsRequired();
        builder.OwnsOne(x => x.Amount, money =>
        {
            money.Property(x => x.Amount).HasColumnName("Amount").HasPrecision(18, 2).IsRequired();
            money.Property(x => x.Currency).HasColumnName("Currency").HasMaxLength(3).IsRequired();
        });
        builder.HasIndex(x => new { x.OrganizationId, x.PaymentAllocationId }).IsUnique()
            .HasDatabaseName("UX_PaymentAllocationReversals_OrganizationId_PaymentAllocationId");
        builder.HasOne<PaymentAllocation>().WithMany()
            .HasForeignKey(x => new { x.OrganizationId, x.PaymentAllocationId })
            .HasPrincipalKey(x => new { x.OrganizationId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
