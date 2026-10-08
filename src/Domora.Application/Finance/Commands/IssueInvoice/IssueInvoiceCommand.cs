using Domora.Domain.Common;
using Domora.Domain.Finance.Enums;

namespace Domora.Application.Finance.Commands.IssueInvoice;

public sealed record IssueInvoiceCommand(
    Guid LeaseId,
    InvoiceType Type,
    Money Amount,
    DateOnly DueDate
);