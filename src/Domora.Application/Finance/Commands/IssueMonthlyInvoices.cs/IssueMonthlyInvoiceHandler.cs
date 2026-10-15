using Domora.Application.Common.Context;
using Domora.Application.Common.Persistence;
using Domora.Domain.Common;
using Domora.Domain.Finance;
using Domora.Domain.Finance.Enums;
using Domora.Domain.Leasing;
using Domora.Domain.Organizations;

namespace Domora.Application.Finance.Commands.IssueMonthlyInvoices;

public sealed class IssueMonthlyInvoicesHandler
{
    private readonly IOrganizationContext _organizationContext;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly ILeaseRepository _leaseRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IUnitOfWork _unitOfWork;

    public IssueMonthlyInvoicesHandler(
        IOrganizationContext organizationContext,
        IOrganizationRepository organizationRepository,
        ILeaseRepository leaseRepository,
        IInvoiceRepository invoiceRepository,
        IUnitOfWork unitOfWork
    )
    {
        _organizationContext = organizationContext;
        _organizationRepository = organizationRepository;
        _leaseRepository = leaseRepository;
        _invoiceRepository = invoiceRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IssueMonthlyInvoicesResponse> Handle(
        IssueMonthlyInvoicesCommand command,
        CancellationToken cancellationToken
    )
    {
        var organizationIds =
            command.TargetOrganizationIds is not null &&
            command.TargetOrganizationIds.Count > 0
                ? command.TargetOrganizationIds
                : await _organizationRepository.GetAllIdsAsync(
                    cancellationToken
                );

        var processedOrganizations = 0;
        var generatedInvoices = 0;

        var periodStart = new DateOnly(
            command.GenerationDate.Year,
            command.GenerationDate.Month,
            1
        );

        var periodEnd = periodStart
            .AddMonths(1)
            .AddDays(-1);

        var dueDate = periodStart.AddDays(4);

        foreach (var organizationId in organizationIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Every organization's billing work must use its own tenant context.
            _organizationContext.Set(organizationId);

            var activeLeases = await _leaseRepository.GetActiveLeasesAsync(
                cancellationToken
            );

            var stagedInvoiceCount = 0;

            foreach (var lease in activeLeases)
            {
                // Check rent independently so a missing rent invoice can
                // still be generated when the garbage invoice already exists.
                var rentExists =
                    await _invoiceRepository.HasInvoiceGeneratedForPeriodAsync(
                        lease.Id,
                        InvoiceType.Rent,
                        periodStart,
                        periodEnd,
                        cancellationToken
                    );

                if (!rentExists)
                {
                    var rentInvoice = Invoice.Create(
                        lease.Id,
                        InvoiceType.Rent,
                        lease.MonthlyRent,
                        dueDate
                    );

                    await _invoiceRepository.AddAsync(
                        rentInvoice,
                        cancellationToken
                    );

                    stagedInvoiceCount++;
                }

                // Garbage is a separate charge with its own idempotency check.
                var garbageExists =
                    await _invoiceRepository.HasInvoiceGeneratedForPeriodAsync(
                        lease.Id,
                        InvoiceType.Garbage,
                        periodStart,
                        periodEnd,
                        cancellationToken
                    );

                if (!garbageExists)
                {
                    var garbageInvoice = Invoice.Create(
                        lease.Id,
                        InvoiceType.Garbage,
                        new Money(100.00m, lease.MonthlyRent.Currency),
                        dueDate
                    );

                    await _invoiceRepository.AddAsync(
                        garbageInvoice,
                        cancellationToken
                    );

                    stagedInvoiceCount++;
                }
            }

            // Save the organization's staged invoices as one unit of work.
            // If there are no missing invoices, there is nothing to save.
            if (stagedInvoiceCount > 0)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                generatedInvoices += stagedInvoiceCount;
            }

            // Count the organization even if it has no active leases.
            processedOrganizations++;
        }

        return new IssueMonthlyInvoicesResponse(
            processedOrganizations,
            generatedInvoices
        );
    }
}