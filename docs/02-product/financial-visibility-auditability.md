# Financial Visibility and Auditability: Milestone Planning

**Status:** Planning only. No implementation is approved by this document.

## Purpose

Let authorized rental-business staff understand what was invoiced, what was received, how receipts were applied, what remains outstanding, and why a financial correction occurred. Current command paths support invoice issuance, payment receipt/allocation, and voiding; read endpoints for those records are not present.

## Workflows to agree

1. Open an invoice from a lease and understand the amount, due date, current settlement status, and remaining balance.
2. Find payments by reference, date, tenant/lease, or status and see received amount, unallocated credit, and allocations.
3. Trace an invoice balance to the active allocations that reduce it, including the effects of a voided payment whose allocation rows remain historical.
4. Open a payment's audit history and explain its receipt, allocation, and void outcome.
5. Handle records after a user's membership is removed while retaining historical actor identity.

## Business decisions required

- Who may view invoices, payments, allocations, and payment-void history? Current financial rules allow Owner and Finance to perform financial commands and deny Member; confirm whether viewing should use the same boundary.
- Which search and filter dimensions are required for the first operational use: lease/tenant, payment reference, date range, invoice status, or another business identifier?
- Should a payment detail show immutable original amount, current unallocated credit, and all allocations including those excluded after void? Confirm terminology and how a void is visually represented.
- What is the canonical outstanding-balance explanation: invoice amount less allocations from active payments, with status derived from that total? Confirm whether manual adjustments or credits are in scope for this milestone (default: out of scope).
- Which actor identity should remain visible after membership removal, and what should happen if the actor's user account is later unavailable?
- What audit events are required for read access, payment receipt/allocation/void, invoice issue, and membership role/ownership changes? Current void records have actor, timestamp, reason; membership changes do not have an equivalent history record.
- What retention, export, and timezone expectations apply to financial audit history?

## Authorization and isolation constraints

- User identity comes from the authenticated JWT subject; organization comes from the validated organization request context.
- Read handlers must perform organization-scoped queries and use the existing persisted membership/financial authorization policy. Do not accept role values from clients or trust organization IDs embedded in records without comparing scope.
- Preserve PostgreSQL RLS and same-organization foreign-key protections. Verify denied reads and empty/no-leak behavior across organizations at both handler/API and PostgreSQL levels.
- The existing matrix allows Owner and Finance to perform view/receipt/allocation/invoice/audit capabilities and reserves voiding and membership management to Owner. Confirm that this matrix also applies to newly introduced read operations.

## Audit information to retain

The current void event already provides organization, payment, actor user, UTC timestamp, and required reason. For each other event, decide whether durable audit requires:

- organization and affected record identifiers;
- event/action type;
- actor user identifier (or an explicit system actor);
- occurrence timestamp in UTC;
- before/after values where business meaning changes;
- a required reason for corrective actions;
- retention and access rules.

This is a set of questions, not a proposal for a general-purpose ledger or event framework. Preserve the existing separation between void, refund, and reallocation; refunds and reallocation are outside this milestone unless separately approved.

## Suggested planning sequence

1. Agree the five business decisions above, especially viewing roles, balance explanation, and audit event scope.
2. Map each requested screen/read to existing records and identify any balance query that must be derived rather than stored.
3. Specify per-organization query behavior and forbidden cross-organization outcomes.
4. Define acceptance examples for active allocations, unallocated credit, partial payment, voided payment, removed actor, and no-history cases.
5. Only then scope a small implementation slice and its tests. Do not combine it with refunds, reallocation, or broad accounting changes without a separate decision.
