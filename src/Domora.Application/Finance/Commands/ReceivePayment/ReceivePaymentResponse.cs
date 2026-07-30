namespace Domora.Application.Finance.Commands.ReceivePayment;

public sealed record ReceivePaymentResponse(
    Guid Id,
    decimal Money,
    string Currency,
    DateTimeOffset PaidAt,
    string Reference
);