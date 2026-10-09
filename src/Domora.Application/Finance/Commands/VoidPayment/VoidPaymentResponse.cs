namespace Domora.Application.Finance.Commands.VoidPayment;

public sealed record VoidPaymentResponse(
    Guid PaymentId,
    string Status
);