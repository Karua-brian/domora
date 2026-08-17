namespace Domora.API.Finances.Payments;

public sealed record ReceivePaymentRequest(
    decimal Amount,
    string Currency,
    DateTimeOffset PaidAt,
    string Reference
    );