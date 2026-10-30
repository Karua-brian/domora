using Domora.Application.Common.Authorization;
using Domora.Application.Common.Exceptions;
using Domora.Application.Finance.Commands.AllocatePayment;
using Domora.Application.Finance.Commands.IssueInvoice;
using Domora.Application.Finance.Commands.ReceivePayment;
using Domora.Application.Finance.Commands.VoidPayment;
using Domora.Domain.Common;
using Domora.Domain.Finance.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Domora.Application.Tests.Authorization;

public sealed class FinancialHandlerAuthorizationTests
{
    [Fact]
    public async Task Direct_handler_calls_require_financial_authorization()
    {
        var financialAuthorization = new Mock<IFinancialAuthorization>();
        financialAuthorization
            .Setup(x => x.EnsureCanAsync(
                It.IsAny<FinancialCapability>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ForbiddenException("denied"));

        var receive = new ReceivePaymentHandler(
            null!, null!, null!, NullLogger<ReceivePaymentHandler>.Instance,
            financialAuthorization.Object);
        var allocate = new AllocatePaymentHandler(
            null!, null!, null!, null!, null!, financialAuthorization.Object);
        var voidPayment = new VoidPaymentHandler(
            null!, null!, null!, null!, null!, null!, null!,
            financialAuthorization.Object);
        var issueInvoice = new IssueInvoiceHandler(
            null!, null!, null!, null!, financialAuthorization.Object);

        await Assert.ThrowsAsync<ForbiddenException>(() => receive.Handle(
            new ReceivePaymentCommand(new Money(10m, "KES"), "AUTH-RECEIVE"),
            CancellationToken.None));
        await Assert.ThrowsAsync<ForbiddenException>(() => allocate.Handle(
            new AllocatePaymentCommand(Guid.NewGuid(), Guid.NewGuid(), new Money(1m, "KES")),
            CancellationToken.None));
        await Assert.ThrowsAsync<ForbiddenException>(() => voidPayment.Handle(
            new VoidPaymentCommand(Guid.NewGuid(), "reason"),
            CancellationToken.None));
        await Assert.ThrowsAsync<ForbiddenException>(() => issueInvoice.Handle(
            new IssueInvoiceCommand(
                Guid.NewGuid(), InvoiceType.Rent, new Money(1m, "KES"), DateOnly.FromDateTime(DateTime.UtcNow)),
            CancellationToken.None));

        financialAuthorization.Verify(x => x.EnsureCanAsync(
            FinancialCapability.ReceivePayments, It.IsAny<CancellationToken>()), Times.Once);
        financialAuthorization.Verify(x => x.EnsureCanAsync(
            FinancialCapability.AllocatePayments, It.IsAny<CancellationToken>()), Times.Once);
        financialAuthorization.Verify(x => x.EnsureCanAsync(
            FinancialCapability.VoidPayments, It.IsAny<CancellationToken>()), Times.Once);
        financialAuthorization.Verify(x => x.EnsureCanAsync(
            FinancialCapability.IssueInvoices, It.IsAny<CancellationToken>()), Times.Once);
    }
}
