namespace Domora.API.Finances.PaymentAllocations;

public sealed record AllocatePaymentRequest(
    Guid PaymentId,
    Guid LeaseId,

    decimal Amount,
    string Currency
);