using Domora.Domain.Finance.Enums;

namespace Domora.Application.Finance.Queries.FinancialVisibility;

public sealed record InvoiceBalanceReadModel(
    Guid InvoiceId, Guid LeaseId, Guid TenantId, Guid UnitId, Guid PropertyId,
    decimal Amount, string Currency, DateOnly DueDate, InvoiceStatus Status,
    decimal AllocatedAmount, decimal OutstandingBalance,
    string? CancellationReason = null, Guid? CancellationActorId = null,
    DateTimeOffset? CancelledAtUtc = null, Guid? ReplacementInvoiceId = null);

public sealed record TenantBalanceReadModel(
    Guid TenantId, string Currency, decimal OutstandingBalance);

public interface IFinancialVisibilityReadRepository
{
    Task<IReadOnlyList<InvoiceBalanceReadModel>> GetInvoicesAsync(Guid organizationId, CancellationToken cancellationToken);
}

public interface IOrganizationLocalDateProvider
{
    DateOnly GetToday(Guid organizationId);
}

public sealed class NairobiOrganizationLocalDateProvider : IOrganizationLocalDateProvider
{
    private static readonly TimeZoneInfo NairobiTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Africa/Nairobi");
    private readonly TimeProvider _timeProvider;

    public NairobiOrganizationLocalDateProvider(TimeProvider? timeProvider = null) =>
        _timeProvider = timeProvider ?? TimeProvider.System;

    public DateOnly GetToday(Guid organizationId) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(_timeProvider.GetUtcNow(), NairobiTimeZone).DateTime);
}
