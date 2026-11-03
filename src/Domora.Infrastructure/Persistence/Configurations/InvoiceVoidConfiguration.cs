using Domora.Domain.Finance;
using Domora.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domora.Infrastructure.Persistence.Configurations;

public sealed class InvoiceVoidConfiguration : IEntityTypeConfiguration<InvoiceVoid>
{
    public void Configure(EntityTypeBuilder<InvoiceVoid> builder)
    {
        builder.ToTable("InvoiceVoids");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OrganizationId).IsRequired();
        builder.Property(x => x.InvoiceId).IsRequired();
        builder.Property(x => x.ReplacementInvoiceId);
        builder.Property(x => x.ActorUserId).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.VoidedAtUtc).IsRequired();

        builder.HasIndex(x => new { x.OrganizationId, x.InvoiceId }).IsUnique()
            .HasDatabaseName("UX_InvoiceVoids_OrganizationId_InvoiceId");
        builder.HasIndex(x => new { x.OrganizationId, x.ReplacementInvoiceId }).IsUnique()
            .HasFilter("\"ReplacementInvoiceId\" IS NOT NULL")
            .HasDatabaseName("UX_InvoiceVoids_OrganizationId_ReplacementInvoiceId");

        builder.HasOne<Invoice>().WithOne()
            .HasForeignKey<InvoiceVoid>(x => new { x.OrganizationId, x.InvoiceId })
            .HasPrincipalKey<Invoice>(x => new { x.OrganizationId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Invoice>().WithMany()
            .HasForeignKey(x => new { x.OrganizationId, x.ReplacementInvoiceId })
            .HasPrincipalKey(x => new { x.OrganizationId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
