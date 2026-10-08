using Domora.Domain.Finance.Enums;

namespace Domora.API.Finances.Invoices;

public sealed record IssueInvoiceRequest(
    Guid LeaseId,
    InvoiceType Type,
    decimal Amount,
    string Currency,
    DateOnly DueDate
);