using Domora.Application.Common.Authorization;
using Domora.Application.Common.Context;
using Domora.Application.Common.Exceptions;
using Domora.Application.Common.Persistence;
using Domora.Application.Finance.Commands.VoidInvoice;
using Domora.Domain.Common;
using Domora.Domain.Common.Exceptions;
using Domora.Domain.Finance;
using Domora.Domain.Finance.Enums;
using Moq;

namespace Domora.Application.Tests.Finance;

public sealed class VoidInvoiceHandlerTests
{
    private static readonly Guid OrganizationId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();

    [Fact]
    public async Task Owner_can_void_unpaid_invoice_and_link_an_existing_replacement()
    {
        var invoice = CreateInvoice();
        var replacement = Invoice.Create(OrganizationId, Guid.NewGuid(), InvoiceType.Water,
            new Money(15m, "KES"), new DateOnly(2026, 10, 1));
        var (handler, voids, _, _) = CreateHandler(invoice, new Money(0m, "KES"), replacement);

        var response = await handler.Handle(new VoidInvoiceCommand(invoice.Id, "Wrong lease", replacement.Id), CancellationToken.None);

        Assert.Equal(InvoiceStatus.Voided, invoice.Status);
        Assert.Equal(replacement.Id, response.ReplacementInvoiceId);
        voids.Verify(x => x.AddAsync(It.Is<InvoiceVoid>(v => v.InvoiceId == invoice.Id &&
            v.ReplacementInvoiceId == replacement.Id && v.ActorUserId == ActorId && v.Reason == "Wrong lease"),
            It.IsAny<CancellationToken>()), Times.Once);
        voids.Verify(x => x.GetByInvoiceIdAsync(OrganizationId, invoice.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Empty_reason_is_rejected_before_transaction_or_writes()
    {
        var (handler, voids, _, unitOfWork) = CreateHandler(CreateInvoice(), new Money(0m, "KES"));
        await Assert.ThrowsAsync<DomainValidationException>(() => handler.Handle(
            new VoidInvoiceCommand(Guid.NewGuid(), " "), CancellationToken.None));
        voids.Verify(x => x.AddAsync(It.IsAny<InvoiceVoid>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(25)]
    [InlineData(100)]
    public async Task Partially_or_fully_allocated_invoice_cannot_be_voided(decimal allocated)
    {
        var invoice = CreateInvoice();
        invoice.RecalculatePaymentStatus(new Money(allocated, "KES"));
        var (handler, voids, _, unitOfWork) = CreateHandler(invoice, new Money(allocated, "KES"));

        await Assert.ThrowsAsync<ResourceConflictException>(() => handler.Handle(
            new VoidInvoiceCommand(invoice.Id, "Incorrect amount"), CancellationToken.None));

        Assert.NotEqual(InvoiceStatus.Voided, invoice.Status);
        voids.Verify(x => x.AddAsync(It.IsAny<InvoiceVoid>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Repeated_void_is_a_conflict()
    {
        var invoice = CreateInvoice();
        invoice.Void(new Money(0m, "KES"));
        var (handler, voids, _, _) = CreateHandler(invoice, new Money(0m, "KES"));

        await Assert.ThrowsAsync<ResourceConflictException>(() => handler.Handle(
            new VoidInvoiceCommand(invoice.Id, "again"), CancellationToken.None));
        voids.Verify(x => x.AddAsync(It.IsAny<InvoiceVoid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Cross_organization_invoice_is_not_found()
    {
        var invoices = new Mock<IInvoiceRepository>();
        invoices.Setup(x => x.GetByIdForOrganizationAsync(It.IsAny<Guid>(), OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Invoice?)null);
        var (handler, voids, _, _) = CreateHandler(CreateInvoice(), new Money(0m, "KES"), invoices: invoices);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
            new VoidInvoiceCommand(Guid.NewGuid(), "cross organization"), CancellationToken.None));
        voids.Verify(x => x.AddAsync(It.IsAny<InvoiceVoid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Finance_and_member_are_denied_by_the_dedicated_capability()
    {
        var authorization = new Mock<IFinancialAuthorization>();
        authorization.Setup(x => x.EnsureCanAsync(FinancialCapability.CancelInvoices, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ForbiddenException("Owner only"));
        var (handler, voids, _, unitOfWork) = CreateHandler(CreateInvoice(), new Money(0m, "KES"), authorization: authorization.Object);

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(
            new VoidInvoiceCommand(Guid.NewGuid(), "reason"), CancellationToken.None));
        voids.Verify(x => x.AddAsync(It.IsAny<InvoiceVoid>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static (VoidInvoiceHandler Handler, Mock<IInvoiceVoidRepository> Voids,
        Mock<IInvoiceRepository> InvoicesVoids, Mock<IUnitOfWork> UnitOfWork) CreateHandler(
        Invoice invoice, Money effectiveAmount, Invoice? replacement = null,
        Mock<IInvoiceRepository>? invoices = null, IFinancialAuthorization? authorization = null)
    {
        invoices ??= new Mock<IInvoiceRepository>();
        invoices.Setup(x => x.GetByIdForOrganizationAsync(invoice.Id, OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);
        if (replacement is not null)
            invoices.Setup(x => x.GetByIdForOrganizationAsync(replacement.Id, OrganizationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(replacement);
        var voids = new Mock<IInvoiceVoidRepository>();
        voids.Setup(x => x.GetByInvoiceIdAsync(OrganizationId, invoice.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InvoiceVoid?)null);
        voids.Setup(x => x.GetByReplacementInvoiceIdAsync(OrganizationId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InvoiceVoid?)null);
        var allocations = new Mock<IPaymentAllocationRepository>();
        allocations.Setup(x => x.GetAllocatedAmountForInvoiceAsync(invoice.Id, "KES", It.IsAny<CancellationToken>(), null))
            .ReturnsAsync(effectiveAmount);
        var transaction = new Mock<ITransaction>();
        transaction.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(transaction.Object);
        unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var organizationContext = new Mock<IOrganizationContext>();
        organizationContext.SetupGet(x => x.OrganizationId).Returns(OrganizationId);
        var userContext = new Mock<IUserContext>();
        userContext.SetupGet(x => x.UserId).Returns(ActorId);

        return (new VoidInvoiceHandler(organizationContext.Object, userContext.Object,
            authorization ?? new AllowFinancialAuthorization(), invoices.Object, voids.Object,
            allocations.Object, unitOfWork.Object), voids, invoices, unitOfWork);
    }

    private static Invoice CreateInvoice() => Invoice.Create(OrganizationId, Guid.NewGuid(),
        InvoiceType.Rent, new Money(100m, "KES"), new DateOnly(2026, 10, 1));

    private sealed class AllowFinancialAuthorization : IFinancialAuthorization
    {
        public Task EnsureCanAsync(FinancialCapability capability, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
