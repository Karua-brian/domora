using Domora.Application.Common.Authorization;
using Domora.Application.Common.Context;
using Domora.Application.Common.Persistence;
using Domora.Application.Finance.Queries.FinancialVisibility;
using Domora.Domain.Finance.Enums;
using Moq;

namespace Domora.Application.Tests.Finance.FinancialVisibility;

public sealed class FinancialVisibilityQueriesTests
{
    private static readonly Guid OrganizationId = Guid.NewGuid();

    private static FinancialVisibilityQueries CreateQueries(
        IReadOnlyList<InvoiceBalanceReadModel> invoices,
        DateOnly today,
        out Mock<IFinancialAuthorization> authorization)
    {
        var organization = new Mock<IOrganizationContext>();
        organization.SetupGet(x => x.OrganizationId).Returns(OrganizationId);
        authorization = new Mock<IFinancialAuthorization>();
        var transaction = new Mock<ITransaction>();
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);
        var repository = new Mock<IFinancialVisibilityReadRepository>();
        repository.Setup(x => x.GetInvoicesAsync(OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoices);
        var dateProvider = new Mock<IOrganizationLocalDateProvider>();
        dateProvider.Setup(x => x.GetToday(OrganizationId)).Returns(today);
        return new FinancialVisibilityQueries(organization.Object, authorization.Object,
            unitOfWork.Object, repository.Object, dateProvider.Object);
    }

    [Fact]
    public async Task Tenant_balances_aggregate_across_leases_by_tenant_and_currency()
    {
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var queries = CreateQueries(new[]
        {
            Invoice(tenantId, Guid.NewGuid(), 100m, 40m),
            Invoice(tenantId, Guid.NewGuid(), 200m, 75m),
            Invoice(otherTenantId, Guid.NewGuid(), 50m, 20m)
        }, new DateOnly(2026, 5, 1), out _);

        var balances = await queries.GetTenantBalancesAsync(CancellationToken.None);

        Assert.Equal(2, balances.Count);
        Assert.Equal(185m, Assert.Single(balances, x => x.TenantId == tenantId).OutstandingBalance);
        Assert.Equal(30m, Assert.Single(balances, x => x.TenantId == otherTenantId).OutstandingBalance);
    }

    [Fact]
    public async Task Arrears_include_only_positive_balances_due_before_organization_local_today()
    {
        var today = new DateOnly(2026, 5, 10);
        var tenantId = Guid.NewGuid();
        var yesterday = Invoice(tenantId, Guid.NewGuid(), 100m, 0m, today.AddDays(-1));
        var dueToday = Invoice(tenantId, Guid.NewGuid(), 100m, 0m, today);
        var tomorrow = Invoice(tenantId, Guid.NewGuid(), 100m, 0m, today.AddDays(1));
        var fullyPaid = Invoice(tenantId, Guid.NewGuid(), 100m, 100m, today.AddDays(-5));
        var partial = Invoice(tenantId, Guid.NewGuid(), 100m, 25m, today.AddDays(-2));
        var queries = CreateQueries(new[] { yesterday, dueToday, tomorrow, fullyPaid, partial }, today, out _);

        var arrears = await queries.GetArrearsAsync(CancellationToken.None);

        Assert.Equal(2, arrears.Count);
        Assert.Contains(arrears, x => x.InvoiceId == yesterday.InvoiceId && x.OutstandingBalance == 100m);
        Assert.Contains(arrears, x => x.InvoiceId == partial.InvoiceId && x.OutstandingBalance == 75m);
        Assert.DoesNotContain(arrears, x => x.InvoiceId == dueToday.InvoiceId);
        Assert.DoesNotContain(arrears, x => x.InvoiceId == tomorrow.InvoiceId);
        Assert.DoesNotContain(arrears, x => x.InvoiceId == fullyPaid.InvoiceId);
    }

    [Fact]
    public async Task Arrears_use_the_local_date_provider_for_the_selected_organization()
    {
        var tenantId = Guid.NewGuid();
        var today = new DateOnly(2026, 7, 2);
        var query = CreateQueries(new[] { Invoice(tenantId, Guid.NewGuid(), 10m, 0m, today) }, today, out _);

        Assert.Empty(await query.GetArrearsAsync(CancellationToken.None));
    }

    [Fact]
    public void Nairobi_date_provider_uses_the_Africa_Nairobi_calendar_date()
    {
        var utcClock = new FixedTimeProvider(new DateTimeOffset(2026, 7, 1, 21, 30, 0, TimeSpan.Zero));
        var provider = new NairobiOrganizationLocalDateProvider(utcClock);

        Assert.Equal(new DateOnly(2026, 7, 2), provider.GetToday(OrganizationId));
    }

    [Fact]
    public async Task Invoice_read_requires_view_financial_records_authorization()
    {
        var queries = CreateQueries(Array.Empty<InvoiceBalanceReadModel>(), new DateOnly(2026, 1, 1), out var authorization);

        await queries.GetInvoicesAsync(CancellationToken.None);

        authorization.Verify(x => x.EnsureCanAsync(FinancialCapability.ViewFinancialRecords,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Tenant_balance_repository_call_is_scoped_to_authenticated_organization()
    {
        var organization = new Mock<IOrganizationContext>();
        organization.SetupGet(x => x.OrganizationId).Returns(OrganizationId);
        var authorization = new Mock<IFinancialAuthorization>();
        var transaction = new Mock<ITransaction>();
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);
        var repository = new Mock<IFinancialVisibilityReadRepository>();
        repository.Setup(x => x.GetInvoicesAsync(OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<InvoiceBalanceReadModel>());
        var dateProvider = new Mock<IOrganizationLocalDateProvider>();
        var queries = new FinancialVisibilityQueries(organization.Object, authorization.Object,
            unitOfWork.Object, repository.Object, dateProvider.Object);

        Assert.Empty(await queries.GetTenantBalancesAsync(CancellationToken.None));
        repository.Verify(x => x.GetInvoicesAsync(OrganizationId, It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static InvoiceBalanceReadModel Invoice(
        Guid tenantId, Guid leaseId, decimal amount, decimal allocated, DateOnly? dueDate = null) =>
        new(Guid.NewGuid(), leaseId, tenantId, Guid.NewGuid(), Guid.NewGuid(), amount, "KES",
            dueDate ?? new DateOnly(2026, 5, 1),
            allocated == amount ? InvoiceStatus.Paid : allocated > 0 ? InvoiceStatus.PartiallyPaid : InvoiceStatus.Pending,
            allocated, amount - allocated);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
