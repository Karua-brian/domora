using Domora.Application.Common.Authorization;
using Domora.Application.Common.Context;
using Domora.Application.Common.Exceptions;
using Domora.Application.Common.Persistence;
using Domora.Domain.Common;
using Domora.Domain.Common.Exceptions;
using Domora.Domain.Finance;
using Domora.Domain.Finance.Enums;

namespace Domora.Application.Finance.Commands.ReversePaymentAllocation;

public sealed class ReversePaymentAllocationHandler(
    IOrganizationContext organizationContext,
    IUserContext userContext,
    IFinancialAuthorization authorization,
    IPaymentAllocationRepository allocations,
    IPaymentRepository payments,
    IInvoiceRepository invoices,
    IUnitOfWork unitOfWork)
{
    public async Task<ReversePaymentAllocationResponse> Handle(
        ReversePaymentAllocationCommand command, CancellationToken cancellationToken)
    {
        await authorization.EnsureCanAsync(FinancialCapability.ReversePaymentAllocations, cancellationToken);
        if (string.IsNullOrWhiteSpace(command.Reason))
            throw new DomainValidationException("An allocation reversal reason is required.");

        var organizationId = organizationContext.OrganizationId;
        if (organizationId == Guid.Empty)
            throw new InvalidOperationException("Organization context is unavailable.");

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        var allocation = await allocations.GetByIdForOrganizationAsync(command.AllocationId, organizationId, cancellationToken);
        if (allocation is null || allocation.OrganizationId != organizationId)
            throw new NotFoundException("Payment allocation not found.");
        if (await allocations.GetReversalByAllocationIdAsync(organizationId, allocation.Id, cancellationToken) is not null)
            throw new ResourceConflictException("This allocation has already been reversed.");

        var payment = await payments.GetByIdAsync(allocation.PaymentId, cancellationToken);
        if (payment is null || payment.OrganizationId != organizationId)
            throw new NotFoundException("Payment not found.");
        if (payment.Status != PaymentStatus.Active)
            throw new ResourceConflictException("An allocation on a voided payment cannot be reversed.");

        var invoice = await invoices.GetByIdAsync(allocation.InvoiceId, cancellationToken);
        if (invoice is null || invoice.OrganizationId != organizationId)
            throw new NotFoundException("Invoice not found.");
        if (allocation.AllocateAmount.Currency != invoice.Amount.Currency ||
            allocation.AllocateAmount.Currency != payment.TotalAmount.Currency)
            throw new ResourceConflictException("Allocation currency does not match its payment and invoice.");

        // Reversal is deliberately full-only; each allocation can have one append-only reversal record.
        var effectiveAllocated = await allocations.GetAllocatedAmountForInvoiceAsync(
            invoice.Id, invoice.Amount.Currency, cancellationToken);
        var newEffectiveAmount = Math.Max(0m, effectiveAllocated.Amount - allocation.AllocateAmount.Amount);
        invoice.RecalculatePaymentStatus(new Money(newEffectiveAmount, invoice.Amount.Currency));
        payment.RestoreCredit(allocation.AllocateAmount);
        var reversal = PaymentAllocationReversal.Create(allocation, command.Reason, userContext.UserId);
        await allocations.AddReversalAsync(reversal, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new ReversePaymentAllocationResponse(reversal.Id, allocation.Id, payment.Id,
            invoice.Id, reversal.Amount.Amount, reversal.Amount.Currency, "Reversed");
    }
}
