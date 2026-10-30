using Domora.Application.Common.Authorization;
using Domora.Application.Common.Context;
using Domora.Application.Common.Exceptions;
using Domora.Application.Common.Persistence;
using Domora.Domain.Finance;
using Domora.Domain.Leasing;

namespace Domora.Application.Finance.Commands.IssueInvoice;

public sealed class IssueInvoiceHandler
{
    private readonly IOrganizationContext _organizationContext;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly ILeaseRepository _leaseRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFinancialAuthorization _financialAuthorization;

    public IssueInvoiceHandler(
        IOrganizationContext organizationContext,
        IInvoiceRepository invoiceRepository,
        ILeaseRepository leaseRepository,
        IUnitOfWork unitOfWork,
        IFinancialAuthorization financialAuthorization
    )
    {
        _organizationContext = organizationContext;
        _invoiceRepository = invoiceRepository;
        _leaseRepository = leaseRepository;
        _unitOfWork = unitOfWork;
        _financialAuthorization = financialAuthorization;
    }

    public async Task<IssueInvoiceResponse> Handle(
        IssueInvoiceCommand command,
        CancellationToken cancellationToken
    )
    {
        // Ensure the user has permission to issue invoices
        await _financialAuthorization.EnsureCanAsync(
            FinancialCapability.IssueInvoices,
            cancellationToken
        );

        await using var transaction = await _unitOfWork.BeginTransactionAsync(
            cancellationToken
        );

        var lease = await _leaseRepository.GetByIdAsync(
            command.LeaseId,
            cancellationToken
        );

        if (lease is null)
            throw new NotFoundException("Lease not found.");

        var invoice = Invoice.Create(
            _organizationContext.OrganizationId,
            lease.Id,
            command.Type,
            command.Amount,
            command.DueDate
        ); 

        await _invoiceRepository.AddAsync(
            invoice,
            cancellationToken
        );

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new IssueInvoiceResponse(
            invoice.Id,
            invoice.LeaseId,
            invoice.Type,
            invoice.Amount.Amount,
            invoice.Amount.Currency,
            invoice.DueDate,
            invoice.Status,
            invoice.Version
        );
    }
}
