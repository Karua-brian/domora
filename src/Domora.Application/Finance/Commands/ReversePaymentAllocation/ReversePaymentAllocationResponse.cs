namespace Domora.Application.Finance.Commands.ReversePaymentAllocation;

public sealed record ReversePaymentAllocationResponse(Guid ReversalId, Guid AllocationId,
    Guid PaymentId, Guid InvoiceId, decimal Amount, string Currency, string Status);
