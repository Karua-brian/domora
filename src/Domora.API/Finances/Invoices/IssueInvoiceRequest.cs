namespace Domora.API.Finances.Invoices;

public sealed record IssueInvoiceRequest(
    Guid LeaseId,
    decimal Amount,
    string Currency,
    DateOnly DueDate
);