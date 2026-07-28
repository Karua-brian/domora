using Domora.Domain.Common;

namespace Domora.Application.Finance.Commands.IssueInvoice;

public sealed record IssueInvoiceCommand(
    Guid LeaseId,
    Money Amount,
    DateOnly DueDate
);