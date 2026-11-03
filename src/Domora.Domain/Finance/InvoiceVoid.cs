using Domora.Domain.Common.Exceptions;

namespace Domora.Domain.Finance;

/// <summary>Immutable audit event for voiding an unpaid invoice.</summary>
public sealed class InvoiceVoid
{
    public Guid Id { get; }
    public Guid OrganizationId { get; }
    public Guid InvoiceId { get; }
    public Guid? ReplacementInvoiceId { get; }
    public Guid ActorUserId { get; }
    public string Reason { get; }
    public DateTimeOffset VoidedAtUtc { get; }

    private InvoiceVoid()
    {
        Reason = null!;
    }

    private InvoiceVoid(Guid id, Guid organizationId, Guid invoiceId, Guid? replacementInvoiceId,
        Guid actorUserId, string reason, DateTimeOffset voidedAtUtc)
    {
        if (id == Guid.Empty || organizationId == Guid.Empty || invoiceId == Guid.Empty || actorUserId == Guid.Empty)
            throw new DomainValidationException("Invoice void identity fields are required.");
        if (replacementInvoiceId == Guid.Empty || replacementInvoiceId == invoiceId)
            throw new DomainValidationException("Replacement invoice reference is invalid.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainValidationException("An invoice void reason is required.");
        if (reason.Trim().Length > 2000)
            throw new DomainValidationException("Invoice void reason cannot exceed 2000 characters.");

        Id = id;
        OrganizationId = organizationId;
        InvoiceId = invoiceId;
        ReplacementInvoiceId = replacementInvoiceId;
        ActorUserId = actorUserId;
        Reason = reason.Trim();
        VoidedAtUtc = voidedAtUtc.ToUniversalTime();
    }

    public static InvoiceVoid Create(Invoice invoice, Guid actorUserId, string reason,
        Guid? replacementInvoiceId = null) => new(Guid.NewGuid(), invoice.OrganizationId,
            invoice.Id, replacementInvoiceId, actorUserId, reason, DateTimeOffset.UtcNow);
}
