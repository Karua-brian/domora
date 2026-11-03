using Domora.Application.Common.Authorization;
using Domora.Application.Common.Context;
using Domora.Application.Common.Exceptions;
using Domora.Application.Common.Persistence;
using Domora.Domain.Common;
using Domora.Domain.Common.Exceptions;
using Domora.Domain.Finance;
using Domora.Domain.Finance.Enums;

namespace Domora.Application.Finance.Commands.VoidInvoice;

public sealed class VoidInvoiceHandler(
    IOrganizationContext organizationContext,
    IUserContext userContext,
    IFinancialAuthorization authorization,
    IInvoiceRepository invoices,
    IInvoiceVoidRepository invoiceVoids,
    IPaymentAllocationRepository allocations,
    IUnitOfWork unitOfWork)
{
    public async Task<VoidInvoiceResponse> Handle(VoidInvoiceCommand command, CancellationToken cancellationToken)
    {
        await authorization.EnsureCanAsync(FinancialCapability.CancelInvoices, cancellationToken);
        if (string.IsNullOrWhiteSpace(command.Reason))
            throw new DomainValidationException("An invoice void reason is required.");

        var organizationId = organizationContext.OrganizationId;
        if (organizationId == Guid.Empty)
            throw new InvalidOperationException("Organization context is unavailable.");

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        var invoice = await invoices.GetByIdForOrganizationAsync(command.InvoiceId, organizationId, cancellationToken);
        if (invoice is null)
            throw new NotFoundException("Invoice not found.");
        if (invoice.Status == InvoiceStatus.Voided ||
            await invoiceVoids.GetByInvoiceIdAsync(organizationId, invoice.Id, cancellationToken) is not null)
            throw new ResourceConflictException("This invoice has already been voided.");

        if (command.ReplacementInvoiceId is { } replacementId)
        {
            if (replacementId == invoice.Id)
                throw new DomainValidationException("An invoice cannot replace itself.");
            var replacement = await invoices.GetByIdForOrganizationAsync(replacementId, organizationId, cancellationToken);
            if (replacement is null)
                throw new NotFoundException("Replacement invoice not found.");
            if (replacement.Status == InvoiceStatus.Voided)
                throw new ResourceConflictException("A voided invoice cannot be linked as a replacement.");
            if (await invoiceVoids.GetByReplacementInvoiceIdAsync(organizationId, replacementId, cancellationToken) is not null)
                throw new ResourceConflictException("The replacement invoice is already linked to another void.");
        }

        var effectiveAllocated = await allocations.GetAllocatedAmountForInvoiceAsync(
            invoice.Id, invoice.Amount.Currency, cancellationToken);
        invoice.Void(effectiveAllocated);

        var voidEvent = InvoiceVoid.Create(invoice, userContext.UserId, command.Reason, command.ReplacementInvoiceId);
        await invoiceVoids.AddAsync(voidEvent, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new VoidInvoiceResponse(invoice.Id, invoice.Status, voidEvent.Id, voidEvent.ReplacementInvoiceId);
    }
}
