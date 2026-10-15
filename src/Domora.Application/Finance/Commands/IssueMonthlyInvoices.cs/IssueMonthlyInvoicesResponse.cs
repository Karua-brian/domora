namespace Domora.Application.Finance.Commands.IssueMonthlyInvoices;

public sealed record IssueMonthlyInvoicesResponse(
    int TotalOrganizationsProcessed,
    int TotalInvoicesGenerated
);
