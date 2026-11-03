using Domora.Domain.Common.Exceptions;
using Domora.Domain.Common;

namespace Domora.Domain.Finance;

/// <summary>An immutable audit record for the full reversal of one allocation.</summary>
public sealed class PaymentAllocationReversal
{
    public Guid Id { get; }
    public Guid OrganizationId { get; }
    public Guid PaymentAllocationId { get; }
    public Money Amount { get; }
    public string Reason { get; }
    public Guid ActorUserId { get; }
    public DateTimeOffset ReversedAtUtc { get; }

    private PaymentAllocationReversal()
    {
        Amount = null!;
        Reason = null!;
    }

    private PaymentAllocationReversal(Guid id, Guid organizationId, Guid allocationId,
        Money amount, string reason, Guid actorUserId, DateTimeOffset reversedAtUtc)
    {
        if (id == Guid.Empty || organizationId == Guid.Empty || allocationId == Guid.Empty || actorUserId == Guid.Empty)
            throw new DomainValidationException("Allocation reversal identity fields are required.");
        if (amount.Amount <= 0)
            throw new DomainValidationException("Allocation reversal amount must be greater than zero.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainValidationException("An allocation reversal reason is required.");
        if (reason.Trim().Length > 2000)
            throw new DomainValidationException("Allocation reversal reason cannot exceed 2000 characters.");

        Id = id;
        OrganizationId = organizationId;
        PaymentAllocationId = allocationId;
        Amount = amount;
        Reason = reason.Trim();
        ActorUserId = actorUserId;
        ReversedAtUtc = reversedAtUtc.ToUniversalTime();
    }

    public static PaymentAllocationReversal Create(PaymentAllocation allocation,
        string reason, Guid actorUserId) => new(Guid.NewGuid(), allocation.OrganizationId,
            allocation.Id, allocation.AllocateAmount, reason, actorUserId, DateTimeOffset.UtcNow);
}
