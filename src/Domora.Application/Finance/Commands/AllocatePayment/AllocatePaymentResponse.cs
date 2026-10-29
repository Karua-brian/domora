namespace Domora.Application.Finance.Commands.AllocatePayment;

public sealed record AllocatePaymentResponse(
    Guid PaymentId,
    decimal TotalAllocated,
    string Currency,
    IReadOnlyCollection<PaymentAllocationResponse> Allocations
);
public sealed record PaymentAllocationResponse(
    Guid Id,
    Guid InvoiceId,
    decimal Amount,
    string Currency
);