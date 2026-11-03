using Domora.Application.Common.Authorization;
using Domora.Application.Common.Context;
using Domora.Application.Common.Exceptions;
using Domora.Application.Common.Persistence;
using Domora.Application.Finance.Commands.AllocatePayment;
using Domora.Application.Finance.Commands.ReceivePayment;
using Domora.Domain.Common;
using Domora.Domain.Common.Exceptions;
using Domora.Domain.Finance;
using Domora.Domain.Finance.Enums;
using Domora.Domain.Leasing;
using Domora.Domain.Properties;
using Domora.Domain.Units;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Domora.Application.Tests.Finance;

public sealed class PaymentEntrySafetyTests
{
    private static readonly Guid OrganizationId = Guid.NewGuid();

    [Fact]
    public async Task Same_reference_and_amount_and_currency_returns_existing_payment()
    {
        var existing = Payment.Receive(OrganizationId, new Money(120m, "KES"), "REF-1");
        var (handler, payments) = CreateReceiveHandler(existing);

        var result = await handler.Handle(
            new ReceivePaymentCommand(new Money(120m, "KES"), "REF-1"),
            CancellationToken.None);

        Assert.Equal(existing.Id, result.Id);
        Assert.Equal(120m, result.Money);
        Assert.Equal("KES", result.Currency);
        payments.Verify(x => x.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Same_reference_with_different_amount_conflicts()
    {
        var existing = Payment.Receive(OrganizationId, new Money(120m, "KES"), "REF-2");
        var (handler, _) = CreateReceiveHandler(existing);

        var error = await Assert.ThrowsAsync<ResourceConflictException>(() => handler.Handle(
            new ReceivePaymentCommand(new Money(121m, "KES"), "REF-2"),
            CancellationToken.None));

        Assert.Contains("different amount or currency", error.Message);
    }

    [Fact]
    public async Task Same_reference_with_different_currency_conflicts()
    {
        var existing = Payment.Receive(OrganizationId, new Money(120m, "KES"), "REF-3");
        var (handler, _) = CreateReceiveHandler(existing);

        await Assert.ThrowsAsync<ResourceConflictException>(() => handler.Handle(
            new ReceivePaymentCommand(new Money(120m, "USD"), "REF-3"),
            CancellationToken.None));
    }

    [Fact]
    public async Task Lease_from_another_organization_is_rejected_before_any_allocation_changes()
    {
        var otherOrganizationId = Guid.NewGuid();
        var payment = Payment.Receive(OrganizationId, new Money(100m, "KES"), "ORG-A-RECEIPT");
        var invoice = Invoice.Create(otherOrganizationId, Guid.NewGuid(), InvoiceType.Rent,
            new Money(100m, "KES"), new DateOnly(2026, 1, 1));
        var payments = new Mock<IPaymentRepository>();
        payments.Setup(x => x.GetByIdAsync(payment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);
        var leases = new Mock<ILeaseRepository>();
        leases.Setup(x => x.GetByIdForOrganizationAsync(
                It.IsAny<Guid>(), OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lease?)null);
        var invoices = new Mock<IInvoiceRepository>();
        var allocations = new Mock<IPaymentAllocationRepository>();
        allocations.Setup(x => x.GetAllocatedAmountForPaymentAsync(
                payment.Id, "KES", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Money(0m, "KES"));
        var unitOfWork = CreateUnitOfWork();
        var organization = new Mock<IOrganizationContext>();
        organization.SetupGet(x => x.OrganizationId).Returns(OrganizationId);
        var handler = new AllocatePaymentHandler(
            payments.Object, leases.Object, invoices.Object, allocations.Object,
            unitOfWork.Object, new AllowFinancialAuthorization(), organization.Object);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
            new AllocatePaymentCommand(payment.Id, Guid.NewGuid(), new Money(50m, "KES")),
            CancellationToken.None));

        leases.Verify(x => x.GetByIdForOrganizationAsync(
            It.IsAny<Guid>(), OrganizationId, It.IsAny<CancellationToken>()), Times.Once);
        invoices.Verify(x => x.GetUnpaidInvoicesByLeaseIdAsync(
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        allocations.Verify(x => x.AddAsync(
            It.IsAny<PaymentAllocation>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal(100m, payment.UnallocatedAmount.Amount);
        Assert.Equal(InvoiceStatus.Pending, invoice.Status);
    }

    private static (ReceivePaymentHandler Handler, Mock<IPaymentRepository> Payments)
        CreateReceiveHandler(Payment existing)
    {
        var payments = new Mock<IPaymentRepository>();
        payments.Setup(x => x.GetByReferenceAsync(
                OrganizationId, existing.Reference, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        var organization = new Mock<IOrganizationContext>();
        organization.SetupGet(x => x.OrganizationId).Returns(OrganizationId);
        return (new ReceivePaymentHandler(
            organization.Object, payments.Object, CreateUnitOfWork().Object,
            NullLogger<ReceivePaymentHandler>.Instance,
            new AllowFinancialAuthorization()), payments);
    }

    private static Mock<IUnitOfWork> CreateUnitOfWork()
    {
        var transaction = new Mock<ITransaction>();
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);
        return unitOfWork;
    }

    private sealed class AllowFinancialAuthorization : IFinancialAuthorization
    {
        public Task EnsureCanAsync(FinancialCapability capability, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
