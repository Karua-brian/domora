using Domora.Domain.Common.Exceptions;

namespace Domora.Domain.Finance;

public sealed class PaymentVoid
{
    public Guid Id { get; }
    public Guid OrganizationId { get; }
    public Guid PaymentId { get; }
    public Guid ActorUserId { get; }
    public string Reason { get; }
    public DateTimeOffset VoidedAtUtc { get; }

    private PaymentVoid()
    {
        Reason = null!;
    }

    private PaymentVoid(
        Guid id,
        Guid organizationId,
        Guid paymentId,
        Guid actorUserId,
        string reason,
        DateTimeOffset voidedAtUtc)
    {
        if (id == Guid.Empty || organizationId == Guid.Empty ||
            paymentId == Guid.Empty || actorUserId == Guid.Empty)
            throw new DomainValidationException(
                "Payment void identity fields are required."
            );

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainValidationException(
                "A payment void reason is required."
            );

        if (reason.Trim().Length > 2000)
            throw new DomainValidationException(
                "Payment void reason cannot exceed 2000 characters."
            );

        Id = id;
        OrganizationId = organizationId;
        PaymentId = paymentId;
        ActorUserId = actorUserId;
        Reason = reason.Trim();
        VoidedAtUtc = voidedAtUtc.ToUniversalTime();
    }

    public static PaymentVoid Create(
        Guid organizationId,
        Guid paymentId,
        Guid actorUserId,
        string reason) => new(
            Guid.NewGuid(), organizationId, paymentId, actorUserId,
            reason, DateTimeOffset.UtcNow);
}
