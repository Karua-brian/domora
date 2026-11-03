using Domora.Application.Common.Authorization;
using Domora.Application.Common.Context;
using Domora.Application.Common.Exceptions;
using Domora.Application.Common.Persistence;
using Domora.Application.Finance.Commands.ReversePaymentAllocation;
using Domora.Domain.Common;
using Domora.Domain.Common.Exceptions;
using Domora.Domain.Finance;
using Domora.Domain.Finance.Enums;
using Moq;

namespace Domora.Application.Tests.Finance;

public sealed class AllocationReversalHandlerTests
{
    private static readonly Guid OrganizationId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();

    [Fact]
    public async Task Full_reversal_preserves_allocation_restores_payment_credit_and_recalculates_invoice()
    {
        var payment = Payment.Receive(OrganizationId, new Money(100m, "KES"), "ALLOC-REV-1");
        payment.DeductCredit(new Money(30m, "KES"));
        var invoice = Invoice.Create(OrganizationId, Guid.NewGuid(), InvoiceType.Rent,
            new Money(100m, "KES"), new DateOnly(2026, 10, 1));
        invoice.RecalculatePaymentStatus(new Money(70m, "KES"));
        var allocation = PaymentAllocation.Allocate(OrganizationId, payment.Id, invoice.Id, new Money(30m, "KES"));
        var allocations = new Mock<IPaymentAllocationRepository>();
        allocations.Setup(x => x.GetByIdForOrganizationAsync(allocation.Id, OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allocation);
        allocations.Setup(x => x.GetAllocatedAmountForInvoiceAsync(invoice.Id, "KES", It.IsAny<CancellationToken>(), null))
            .ReturnsAsync(new Money(70m, "KES"));
        var payments = new Mock<IPaymentRepository>();
        payments.Setup(x => x.GetByIdAsync(payment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(payment);
        var invoices = new Mock<IInvoiceRepository>();
        invoices.Setup(x => x.GetByIdAsync(invoice.Id, It.IsAny<CancellationToken>())).ReturnsAsync(invoice);
        var unitOfWork = CreateUnitOfWork();
        var handler = CreateHandler(allocations, payments, invoices, unitOfWork);

        var response = await handler.Handle(new ReversePaymentAllocationCommand(allocation.Id, "Wrong invoice"), CancellationToken.None);

        Assert.Equal(30m, response.Amount);
        Assert.Equal(100m, payment.UnallocatedAmount.Amount);
        Assert.Equal(InvoiceStatus.PartiallyPaid, invoice.Status);
        allocations.Verify(x => x.AddReversalAsync(It.Is<PaymentAllocationReversal>(r =>
            r.PaymentAllocationId == allocation.Id && r.Amount.Amount == 30m && r.Reason == "Wrong invoice" && r.ActorUserId == ActorId),
            It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Blank_reason_is_rejected_without_writing()
    {
        var allocations = new Mock<IPaymentAllocationRepository>();
        var unitOfWork = CreateUnitOfWork();
        var handler = CreateHandler(allocations, new Mock<IPaymentRepository>(), new Mock<IInvoiceRepository>(), unitOfWork);

        await Assert.ThrowsAsync<DomainValidationException>(() => handler.Handle(
            new ReversePaymentAllocationCommand(Guid.NewGuid(), "  "), CancellationToken.None));

        unitOfWork.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        allocations.Verify(x => x.AddReversalAsync(It.IsAny<PaymentAllocationReversal>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Existing_reversal_and_voided_payment_are_rejected_without_second_reversal()
    {
        var payment = Payment.Receive(OrganizationId, new Money(100m, "KES"), "ALLOC-REV-2");
        var invoice = Invoice.Create(OrganizationId, Guid.NewGuid(), InvoiceType.Rent, new Money(100m, "KES"), new DateOnly(2026, 10, 1));
        var allocation = PaymentAllocation.Allocate(OrganizationId, payment.Id, invoice.Id, new Money(30m, "KES"));
        var allocations = new Mock<IPaymentAllocationRepository>();
        allocations.Setup(x => x.GetByIdForOrganizationAsync(allocation.Id, OrganizationId, It.IsAny<CancellationToken>())).ReturnsAsync(allocation);
        allocations.Setup(x => x.GetReversalByAllocationIdAsync(OrganizationId, allocation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(PaymentAllocationReversal.Create(allocation, "already reversed", ActorId));
        var unitOfWork = CreateUnitOfWork();
        var handler = CreateHandler(allocations, new Mock<IPaymentRepository>(), new Mock<IInvoiceRepository>(), unitOfWork);

        await Assert.ThrowsAsync<ResourceConflictException>(() => handler.Handle(
            new ReversePaymentAllocationCommand(allocation.Id, "again"), CancellationToken.None));
        allocations.Verify(x => x.AddReversalAsync(It.IsAny<PaymentAllocationReversal>(), It.IsAny<CancellationToken>()), Times.Never);

        allocations.Setup(x => x.GetReversalByAllocationIdAsync(OrganizationId, allocation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentAllocationReversal?)null);
        payment.Void();
        var payments = new Mock<IPaymentRepository>();
        payments.Setup(x => x.GetByIdAsync(payment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(payment);
        handler = CreateHandler(allocations, payments, new Mock<IInvoiceRepository>(), CreateUnitOfWork());
        await Assert.ThrowsAsync<ResourceConflictException>(() => handler.Handle(
            new ReversePaymentAllocationCommand(allocation.Id, "voided"), CancellationToken.None));
    }

    [Fact]
    public async Task Cross_organization_allocation_is_not_found()
    {
        var allocations = new Mock<IPaymentAllocationRepository>();
        allocations.Setup(x => x.GetByIdForOrganizationAsync(It.IsAny<Guid>(), OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentAllocation?)null);
        var unitOfWork = CreateUnitOfWork();
        var handler = CreateHandler(allocations, new Mock<IPaymentRepository>(), new Mock<IInvoiceRepository>(), unitOfWork);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
            new ReversePaymentAllocationCommand(Guid.NewGuid(), "cross org"), CancellationToken.None));
        allocations.Verify(x => x.AddReversalAsync(It.IsAny<PaymentAllocationReversal>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Finance_role_is_not_granted_reversal_capability()
    {
        var authorization = new Mock<IFinancialAuthorization>();
        authorization.Setup(x => x.EnsureCanAsync(FinancialCapability.ReversePaymentAllocations, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ForbiddenException("Owner only"));
        var unitOfWork = CreateUnitOfWork();
        var handler = CreateHandler(new Mock<IPaymentAllocationRepository>(), new Mock<IPaymentRepository>(),
            new Mock<IInvoiceRepository>(), unitOfWork, authorization.Object);

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(
            new ReversePaymentAllocationCommand(Guid.NewGuid(), "reason"), CancellationToken.None));
        unitOfWork.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static ReversePaymentAllocationHandler CreateHandler(Mock<IPaymentAllocationRepository> allocations,
        Mock<IPaymentRepository> payments, Mock<IInvoiceRepository> invoices, Mock<IUnitOfWork> unitOfWork,
        IFinancialAuthorization? authorization = null)
    {
        var organization = new Mock<IOrganizationContext>();
        organization.SetupGet(x => x.OrganizationId).Returns(OrganizationId);
        var user = new Mock<IUserContext>();
        user.SetupGet(x => x.UserId).Returns(ActorId);
        return new ReversePaymentAllocationHandler(organization.Object, user.Object,
            authorization ?? new AllowFinancialAuthorization(), allocations.Object, payments.Object, invoices.Object, unitOfWork.Object);
    }

    private static Mock<IUnitOfWork> CreateUnitOfWork()
    {
        var transaction = new Mock<ITransaction>();
        transaction.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(transaction.Object);
        unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        return unitOfWork;
    }

    private sealed class AllowFinancialAuthorization : IFinancialAuthorization
    {
        public Task EnsureCanAsync(FinancialCapability capability, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
