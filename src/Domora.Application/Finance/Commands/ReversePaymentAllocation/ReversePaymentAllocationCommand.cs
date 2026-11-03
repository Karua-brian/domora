namespace Domora.Application.Finance.Commands.ReversePaymentAllocation;

public sealed record ReversePaymentAllocationCommand(Guid AllocationId, string Reason);
