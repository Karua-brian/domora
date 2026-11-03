# Financial visibility MVP

Invoice balances are derived from invoice amounts less allocations belonging to active payments. Invoice voiding is not supported by the current domain or schema and is excluded from this milestone; support for voided invoices is a future requirement.

Arrears begin the day after an invoice due date. An invoice due today is not overdue. The MVP resolves the organization's current date using `Africa/Nairobi` through `IOrganizationLocalDateProvider`, which isolates timezone selection for future organization-specific configuration.
