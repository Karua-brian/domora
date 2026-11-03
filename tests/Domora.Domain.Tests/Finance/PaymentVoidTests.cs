using Domora.Domain.Common;
using Domora.Domain.Common.Exceptions;
using Domora.Domain.Finance;
using Domora.Domain.Finance.Enums;

namespace Domora.Domain.Tests.Finance;

public sealed class PaymentVoidTests
{
    [Fact]
    public void Creating_void_records_actor_reason_organization_and_utc_timestamp()
    {
        var before = DateTimeOffset.UtcNow;
        var paymentVoid = PaymentVoid.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "  Entered in error  ");

        Assert.Equal("Entered in error", paymentVoid.Reason);
        Assert.NotEqual(Guid.Empty, paymentVoid.OrganizationId);
        Assert.NotEqual(Guid.Empty, paymentVoid.PaymentId);
        Assert.NotEqual(Guid.Empty, paymentVoid.ActorUserId);
        Assert.Equal(TimeSpan.Zero, paymentVoid.VoidedAtUtc.Offset);
        Assert.InRange(paymentVoid.VoidedAtUtc, before, DateTimeOffset.UtcNow);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Creating_void_rejects_blank_reason(string reason)
    {
        Assert.Throws<DomainValidationException>(() =>
            PaymentVoid.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), reason));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public void Voided_payment_cannot_be_allocated_even_for_zero_amount(decimal requested)
    {
        var payment = Payment.Receive(Guid.NewGuid(), new Money(100, "KES"), "test");
        payment.Void();

        Assert.Throws<ResourceConflictException>(() => payment.EnsureCanAllocate(
            new Money(requested, "KES"), new Money(0, "KES")));
        Assert.Equal(PaymentStatus.Voided, payment.Status);
        Assert.Equal(100m, payment.UnallocatedAmount.Amount);
    }

    [Fact]
    public void Restoring_allocation_credit_increases_available_balance_but_never_above_total()
    {
        var payment = Payment.Receive(Guid.NewGuid(), new Money(100m, "KES"), "restore-test");
        payment.DeductCredit(new Money(40m, "KES"));

        payment.RestoreCredit(new Money(25m, "KES"));

        Assert.Equal(85m, payment.UnallocatedAmount.Amount);
        Assert.Throws<DomainValidationException>(() => payment.RestoreCredit(new Money(20m, "KES")));
    }
}
