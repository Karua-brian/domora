namespace Domora.API.Finances.PaymentAllocations;

public sealed record AllocatePaymentRequest(
    Guid PaymentId,
    Guid InvoiceId,

    decimal Amount,
    string Currency
);