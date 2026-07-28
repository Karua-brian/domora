using Domora.Domain.Finance;
using Domora.Domain.Leasing;

namespace Domora.Application.Finance.Commands.IssueInvoice;

public sealed class IssueInvoiceHandler
{
    private readonly IInvoiceRepository _invoiceRepository;

    private readonly ILeaseRepository _leaseRepository;

    public IssueInvoiceHandler(
        IInvoiceRepository invoiceRepository,
        ILeaseRepository leaseRepository
    )
    {
        _invoiceRepository = invoiceRepository;
        _leaseRepository = leaseRepository;
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

        return new IssueInvoiceResponse(
            invoice.Id,
            invoice.LeaseId,
            invoice.Amount.Amount,
            invoice.DueDate,
            invoice.Status
        );
    }
}