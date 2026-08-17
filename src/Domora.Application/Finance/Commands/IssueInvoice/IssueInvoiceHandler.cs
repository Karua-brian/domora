using Domora.Application.Common.Persistence;
using Domora.Domain.Finance;
using Domora.Domain.Leasing;

namespace Domora.Application.Finance.Commands.IssueInvoice;

public sealed class IssueInvoiceHandler
{
    private readonly IInvoiceRepository _invoiceRepository;

    private readonly ILeaseRepository _leaseRepository;

    private readonly IUnitOfWork _unitOfWork;

    public IssueInvoiceHandler(
        IInvoiceRepository invoiceRepository,
        ILeaseRepository leaseRepository,
        IUnitOfWork unitOfWork
    )
    {
        _invoiceRepository = invoiceRepository;
        _leaseRepository = leaseRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IssueInvoiceResponse> Handle(
        IssueInvoiceCommand command,
        CancellationToken cancellationToken
    )
    {
        var lease = await _leaseRepository.GetByIdAsync(
            command.LeaseId,
            cancellationToken
        );

        if (lease is null)
            throw new InvalidOperationException("Lease not found.");

        var invoice = Invoice.Create(
            lease.Id,
            command.Amount,
            command.DueDate
        ); 

        await _invoiceRepository.AddAsync(
            invoice,
            cancellationToken
        );

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new IssueInvoiceResponse(
            invoice.Id,
            invoice.LeaseId,
            invoice.Amount.Amount,
            invoice.Amount.Currency,
            invoice.DueDate,
            invoice.Status,
            invoice.Version
        );
    }
}