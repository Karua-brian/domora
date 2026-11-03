using Domora.Application.Common.Authorization;
using Domora.Application.Common.Context;
using Domora.Application.Common.Persistence;
using Domora.Domain.Finance.Enums;

namespace Domora.Application.Finance.Queries.FinancialVisibility;

public sealed class FinancialVisibilityQueries(
    IOrganizationContext organizationContext,
    IFinancialAuthorization authorization,
    IUnitOfWork unitOfWork,
    IFinancialVisibilityReadRepository repository,
    IOrganizationLocalDateProvider localDateProvider)
{
    public async Task<IReadOnlyList<InvoiceBalanceReadModel>> GetInvoicesAsync(CancellationToken cancellationToken)
    {
        await authorization.EnsureCanAsync(FinancialCapability.ViewFinancialRecords, cancellationToken);
        var organizationId = RequireOrganizationId();
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        return await repository.GetInvoicesAsync(organizationId, cancellationToken);
    }

    public async Task<IReadOnlyList<TenantBalanceReadModel>> GetTenantBalancesAsync(CancellationToken cancellationToken)
    {
        var invoices = await GetInvoicesAsync(cancellationToken);
        return invoices.Where(x => x.Status != InvoiceStatus.Voided)
            .GroupBy(x => new { x.TenantId, x.Currency })
            .Select(group => new TenantBalanceReadModel(group.Key.TenantId, group.Key.Currency,
                group.Sum(x => x.OutstandingBalance)))
            .OrderBy(x => x.TenantId).ThenBy(x => x.Currency).ToList();
    }

    public async Task<IReadOnlyList<InvoiceBalanceReadModel>> GetArrearsAsync(CancellationToken cancellationToken)
    {
        var organizationId = RequireOrganizationId();
        var today = localDateProvider.GetToday(organizationId);
        var invoices = await GetInvoicesAsync(cancellationToken);
        return invoices.Where(x => x.DueDate < today && x.OutstandingBalance > 0).ToList();
    }

    private Guid RequireOrganizationId()
    {
        var id = organizationContext.OrganizationId;
        if (id == Guid.Empty) throw new InvalidOperationException("Organization context is unavailable.");
        return id;
    }
}
