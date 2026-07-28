namespace Domora.API.Invoices;

public sealed record IssueInvoiceRequest(
    Guid LeaseId,
    decimal Amount,
    string Currency,
    DateOnly DueDate
);