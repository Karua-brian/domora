namespace Domora.Application.Finance.Commands.AllocatePayment;

public sealed record AllocatePaymentResponse(
    Guid Id,
    Guid PaymentId,
    Guid InvoiceId,
    decimal Money,
    string Currency
);