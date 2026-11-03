using Domora.Domain.Common;
using Domora.Domain.Common.Exceptions;
using Domora.Domain.Finance;
using Domora.Domain.Finance.Enums;

namespace Domora.Domain.Tests.Finance;

public sealed class InvoiceVoidTests
{
    [Fact]
    public void Unpaid_invoice_can_be_voided_without_changing_its_original_terms()
    {
        var organizationId = Guid.NewGuid();
        var leaseId = Guid.NewGuid();
        var dueDate = new DateOnly(2026, 10, 15);
        var amount = new Money(125m, "KES");
        var invoice = Invoice.Create(organizationId, leaseId, InvoiceType.Rent, amount, dueDate);

        invoice.Void(new Money(0m, "KES"));

        Assert.Equal(InvoiceStatus.Voided, invoice.Status);
        Assert.Equal(organizationId, invoice.OrganizationId);
        Assert.Equal(leaseId, invoice.LeaseId);
        Assert.Equal(amount, invoice.Amount);
        Assert.Equal(dueDate, invoice.DueDate);
    }

    [Fact]
    public void Invoice_with_effective_allocations_cannot_be_voided()
    {
        var invoice = CreateInvoice();

        Assert.Throws<ResourceConflictException>(() => invoice.Void(new Money(10m, "KES")));
        Assert.Equal(InvoiceStatus.Pending, invoice.Status);
    }

    [Fact]
    public void Voided_invoice_cannot_be_allocated_or_voided_again()
    {
        var invoice = CreateInvoice();
        invoice.Void(new Money(0m, "KES"));

        Assert.Throws<ResourceConflictException>(() => invoice.AllocatePayment(
            new Money(20m, "KES"), new Money(0m, "KES")));
        Assert.Throws<ResourceConflictException>(() => invoice.Void(new Money(0m, "KES")));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Void_event_requires_a_reason(string reason)
    {
        var invoice = CreateInvoice();

        Assert.Throws<DomainValidationException>(() => InvoiceVoid.Create(invoice,
            Guid.NewGuid(), reason));
    }

    [Fact]
    public void Void_event_preserves_actor_reason_timestamp_and_replacement_link()
    {
        var invoice = CreateInvoice();
        var actorId = Guid.NewGuid();
        var replacementId = Guid.NewGuid();
        var before = DateTimeOffset.UtcNow;

        var invoiceVoid = InvoiceVoid.Create(invoice, actorId, "  Wrong charge  ", replacementId);

        Assert.Equal(invoice.OrganizationId, invoiceVoid.OrganizationId);
        Assert.Equal(invoice.Id, invoiceVoid.InvoiceId);
        Assert.Equal(actorId, invoiceVoid.ActorUserId);
        Assert.Equal(replacementId, invoiceVoid.ReplacementInvoiceId);
        Assert.Equal("Wrong charge", invoiceVoid.Reason);
        Assert.Equal(TimeSpan.Zero, invoiceVoid.VoidedAtUtc.Offset);
        Assert.InRange(invoiceVoid.VoidedAtUtc, before, DateTimeOffset.UtcNow);
    }

    private static Invoice CreateInvoice() => Invoice.Create(Guid.NewGuid(), Guid.NewGuid(),
        InvoiceType.Rent, new Money(100m, "KES"), new DateOnly(2026, 10, 1));
}
