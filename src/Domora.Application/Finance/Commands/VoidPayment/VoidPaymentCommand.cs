namespace Domora.Application.Finance.Commands.VoidPayment;

public sealed record VoidPaymentCommand(
    Guid PaymentId
);