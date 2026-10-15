namespace Domora.Application.Finance.Commands.IssueMonthlyInvoices;

public sealed record IssueMonthlyInvoicesCommand(
    DateOnly GenerationDate,
        IReadOnlyCollection<Guid>? TargetOrganizationIds = null
);
