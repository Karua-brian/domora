using Domora.Domain.Finance.Enums;

namespace Domora.Application.Finance.Commands.IssueInvoice;

public sealed record IssueInvoiceResponse(
    Guid Id,
    Guid LeaseId,
    decimal Money,
    string Currency,
    DateOnly DueDate,
    InvoiceStatus Status
);