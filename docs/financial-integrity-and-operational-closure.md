# Financial integrity and operational closure

## Scope and status

This is an implementation assessment, not an accounting policy decision or a code change. Existing behavior is labeled **Current**; proposed behavior is labeled **Proposed**. No database or tests were run for this investigation.

## Current implementation findings

### Domain and persistence

- **Invoices:** `Invoice` stores organization, lease, amount/currency, due date, type, status, and a concurrency version. The only statuses are `Pending`, `PartiallyPaid`, and `Paid`. There is no invoice void, cancellation, credit-note, or replacement relationship. `Amount`, `LeaseId`, and `DueDate` are immutable in the domain entity. `RecalculatePaymentStatus` derives status from an effective allocation total. (`src/Domora.Domain/Finance/Invoice.cs`, `InvoiceStatus.cs`)
- **Payments:** `Payment` belongs to an organization and records total amount, unallocated amount, paid-at UTC, reference, active/voided status, and concurrency version. It does not directly identify a tenant or lease. Its amount and reference cannot be edited. (`Payment.cs`)
- **Allocations:** `PaymentAllocation` records an organization, payment, invoice, and positive amount. There is no allocation status, reversal, or actor/reason. Composite foreign keys require both referenced records to belong to the same organization, and deletion is restricted. (`PaymentAllocation.cs`, `PaymentAllocationConfiguration.cs`)
- **Constraints:** Payment references are unique per organization. Invoice rows are unique by `(LeaseId, Type, DueDate)`, preventing exact duplicates on that key. Monthly billing checks the period for existing invoices before adding rent and garbage invoices. These controls do not identify every semantic duplicate. (`20261015075123_AddOrganizationPaymentUniqueIndex.cs`, `20261010160633_AddUniqueIndexForInvoiceDueDate.cs`, `IssueMonthlyInvoiceHandler.cs`)
- **Money:** `Money` rejects negative values. That is appropriate for receipts and allocations, but a credit or refund must be represented as a separate positive amount and event, not a negative `Money` value. (`src/Domora.Domain/Common/Money.cs`)

### Workflows and invariants

- `ReceivePaymentHandler` authorizes `ReceivePayments`, opens an organization transaction, and treats a repeated organization/reference as an idempotent retry. It returns the original payment without comparing the retry's amount or currency to the original. A unique index handles concurrent duplicate references. (`ReceivePaymentHandler.cs`)
- `AllocatePaymentHandler` authorizes `AllocatePayments`, rejects voided payments, checks payment remainder, and allocates across unpaid invoices for the selected lease in due-date order, then rent/water/garbage order. Invoice allocation caps each entry at that invoice's outstanding amount, and allocations across the command cannot exceed the requested amount or payment remainder. It updates invoice status and the payment's cached unallocated amount in the same transaction. The repository sums allocation totals under RLS. The handler's explicit lease/payment organization comparison is commented out; the current safety boundary therefore also depends on the transaction's RLS-scoped lookups and database organization constraints. (`AllocatePaymentHandler.cs`, `PaymentAllocationRepository.cs`)
- `VoidPaymentHandler` is implemented. It permits only `VoidPayments`, requires a reason, checks organization ownership, and rejects repeat voids. It retains allocation rows, marks the payment voided, recalculates each affected invoice using allocations from active payments only, and inserts a `PaymentVoid` containing actor, reason, organization, and UTC time. (`VoidPaymentHandler.cs`, `PaymentVoid.cs`)
- `PaymentVoid` is the only financial-specific audit event found. No general audit-event framework or audit records for invoice issuance, payment receipt, or allocation were found. Payment `PaidAt` is a timestamp, not an actor/audit record.
- Invoice allocations must remain within the payment's remaining amount and invoice's outstanding amount for the supported handler path. Partial payments are supported. Voided-payment allocations remain historically queryable but do not reduce invoice balances. A void does not restore the payment's cached unallocated amount; the voided payment cannot be allocated again, so that cache is no longer operationally spendable.
- `IssueInvoiceHandler` authorizes issue, obtains the lease through the organization-scoped transaction, and creates an invoice for the current organization. The database trigger rejects invoices whose organization differs from the lease's property organization. (`IssueInvoiceHandler.cs`, `20261020062412_EnforceInvoiceLeaseOrganization.cs`)
- Monthly billing sets each organization's context and transaction before processing it. Its rent/garbage generation checks avoid repeats for a lease/type/period. (`IssueMonthlyInvoiceHandler.cs`)

### Authorization and isolation

- Owner can perform every defined financial capability. Finance can receive payments, allocate payments, issue invoices, and view records. Finance cannot void payments; Member has no financial capability. (`FinancialAuthorization.cs`, `FinancialCapability.cs`)
- Organization RLS is enabled and forced for properties, invoices, payments, payment allocations, and payment voids. Units and leases are scoped through the owning property's organization. Allocation composite foreign keys prevent linking a payment from one organization to an invoice from another. (`20260921061815_AddOrganizationRowLevelSecurity.cs`, `20261017084715_AddPaymentRowLevelSecurity.cs`, `20261017172133_AddInvoicesRowLevelSecurity.cs`, `20261025125344_AddOrganizationIntegrityToPaymentAllocations.cs`, `20261027163658_EnforceUnitOrganizationIsolation.cs`, `20261027171032_EnforceLeaseOrganizationIsolation.cs`, `20261030120000_AddPaymentVoids.cs`)
- `OrganizationTransactionInterceptor` sets `app.organization_id` transaction-locally when `IOrganizationContext` has an organization. `UnitOfWork.BeginTransactionAsync` rejects an empty organization context. A query/write must establish the authenticated organization context before opening its transaction; headers alone do not establish authorization.

## Payment correction scenarios

| Scenario | Business meaning and recommended treatment | Current behavior and limitations |
|---|---|---|
| Wrong amount recorded | Correct the recorded receipt to match the actual bank/cash movement. If no funds were received, void the erroneous receipt and record no replacement. If the real receipt differs from the entry, preserve the correction link and record the actual receipt amount. | No amount edit exists. Owner can void the entire payment and a new payment can be recorded, but there is no structured correction link. Same-reference retry returns the original even if amount/currency differs. **Proposed:** distinguish a false receipt from an amount correction; require a reason and link the replacement to the voided record. |
| Payment associated with wrong tenant or lease | The receipt itself may still be valid; the allocation destination is wrong. Reverse the affected allocation and allocate the same payment to the correct invoice/lease. | Payments have no tenant/lease field. Allocations have no reversal. Voiding the entire payment removes all its allocations from balances, including correct allocations to other invoices. **Proposed:** correct allocations independently while retaining the payment receipt. |
| Allocated to wrong invoice | Correct the allocation destination, not the receipt. Reverse the original allocation, then create a replacement allocation in one transaction. Recalculate both invoices from effective allocations. | Existing voiding is payment-wide. No allocation removal/reversal endpoint or audit exists. **Proposed:** append an allocation reversal linked to the original allocation and append a new allocation; do not delete history. |
| Duplicate payment | A repeated API request with the same organization/reference is a retry; a second bank movement is a second real receipt. A false duplicate entry should be voided; two actual transfers should remain receipts and be allocated, held as tenant credit, or refunded according to the customer's instruction. | Same reference is idempotent, but payload mismatch is silently treated as the same receipt. Distinct references can create duplicate records. **Proposed:** reject same-reference amount/currency mismatch, and provide an explicit duplicate investigation/correction path; never infer a real-world duplicate from matching amount alone. |
| Recorded but never received | The receipt was a data-entry error, not cash. Void it and reverse its allocations; preserve the reason and original payment. | Current Owner-only payment void supports this case and persists actor/reason/time. This is the strongest existing correction workflow. It does not provide an evidence/reference field or supervisory approval. |
| Received then reversed or refunded | A bank reversal/chargeback and a landlord/tenant refund are cash movements after a real receipt. Record the outflow/reversal as a new linked event. A refund may be partial; it does not automatically mean every allocation should be erased. Determine whether the amount is a tenant credit, a reduced receivable, or settled refund. | No refund/reversal event or endpoint exists. Voiding the original receipt loses the distinction between “never received” and “received then paid back.” **Proposed:** model a separate cash-out/reversal with amount, currency, date, reason, reference, actor, and links to source payment and any allocation effects. |

### Payment authorization and audit proposal

**Current:** Owner alone may void a payment. Owner and Finance may receive and allocate. `PaymentVoid` captures actor, reason, timestamp, and organization; receipt/allocation creation has no actor audit.

**Proposed:** keep payment void Owner-only for the first correction slice. Let Owner and Finance correct allocations only if the business accepts finance-role correction; otherwise start Owner-only. Refunds and bank reversals should have a distinct capability, reason, amount, effective date, external reference, actor, and immutable relationship to the original payment. Consider a second approver for refunds only if operational policy requires it; do not add a generic “void any financial record” permission.

## Invoice adjustment scenarios

| Scenario | Business treatment | Current behavior and implications |
|---|---|---|
| Wrong amount | For a wholly unpaid invoice, cancel/void the erroneous invoice and issue a corrected replacement. For a partially paid invoice, establish whether the excess is a tenant credit/refund or whether the remaining debt changes; use a credit adjustment and preserve all payment history. | Invoice amount is immutable, and no void/credit exists. Reissuing at a different amount under the same lease/type/due date is blocked by the unique index until the original is explicitly addressed. |
| Wrong lease | Correct the original billing on the wrong lease and issue on the right lease. If payment was already applied, reverse/reallocate the allocation as a separate correction. Do not move an invoice's lease foreign key in place: that changes historical liability ownership. | No invoice move/void workflow exists. Invoice-to-lease organization integrity and RLS are enforced. |
| Issued twice | Keep one valid invoice and cancel the duplicate; retain a reason and link to the surviving invoice. If both have been paid, apply the excess as tenant credit/refund rather than deleting either receipt. | The unique `(LeaseId, Type, DueDate)` index blocks exact-key duplicates, and monthly billing is period-idempotent. This does not cover duplicates with another due date/type or imported duplicates. |
| No longer payable | If unpaid, issue a cancellation/void event with reason and actor. If partly/fully paid, calculate the resulting tenant credit/refund and reconcile allocations before closing. | No void status or workflow. The current status enum cannot represent cancellation. Financial Visibility explicitly excludes invoice voiding today. |
| Partially paid invoice | Preserve the payment receipt. Apply a credit adjustment to the invoice balance or reverse and reallocate payments only when the allocation was wrong. If the adjusted invoice total is below what was paid, record the surplus as credit/refund. | Payment void can remove all allocations for a payment, but that is too broad for invoice correction. There is no credit-note or allocation reversal model. |

**Proposed accounting boundary:** an invoice correction changes the amount owed; an allocation correction changes which invoice a valid receipt satisfies; a payment void says the receipt was never valid; a refund/reversal records money leaving after a valid receipt. These should remain separate events.

## Minimal implementation sequence

### 1. Close existing payment-entry safety gaps

- **Rules:** Same-reference retries are accepted only when amount/currency match the original; mismatches conflict. Every command remains explicitly scoped to the authenticated organization.
- **Domain/application:** Keep `Payment` immutable. Add an explicit amount/reference comparison to the retry path and an explicit lease/payment organization guard in allocation.
- **API/persistence:** No endpoint or schema change required.
- **Authorization/audit:** Existing Receive/Allocate permissions; log/record no new financial event for an idempotent retry.
- **Acceptance:** Identical retry returns the same payment; mismatched retry returns conflict; cross-org lease/payment cannot allocate; concurrent duplicate reference still creates one payment.

### 2. Add allocation reversal and reallocation

- **Rules:** Reverse only a specified allocation; preserve the payment and original allocation; perform reversal and replacement atomically when correcting a destination. Recompute both invoice statuses from active effective allocations.
- **Domain/application:** Add an immutable allocation reversal (or equivalent append-only allocation status event) with original allocation, amount, reason, actor, and timestamp. Prevent reversal totals exceeding the allocation.
- **API:** Add a specific endpoint such as `POST /api/finances/payment-allocations/{id}/reverse`; a replacement allocation remains a separate operation or is bundled in an atomic correction command.
- **Persistence:** A reversal table/event and RLS policy are genuinely needed if reversals must be auditable without deleting allocations. Composite organization foreign keys to allocation/payment/invoice; no cascaded deletion.
- **Authorization/audit:** Initially Owner-only or explicitly approved Finance capability; capture actor, reason, time, org, original allocation, and replacement allocation.
- **Acceptance:** Wrong-invoice correction preserves the receipt and both allocation histories; affected balances/statuses update once; duplicate/excess reversal and cross-org reversal fail; concurrent correction cannot over-apply.

### 3. Add invoice cancellation for unpaid invoices and replacement links

- **Rules:** Only an unpaid invoice can be voided in this first slice. Paid or partially paid invoices require the next adjustment/credit workflow. A replacement does not erase or mutate the original.
- **Domain/application:** Add a void/cancel state and an immutable invoice void event with reason/actor/time; disallow allocations to canceled invoices. Link an optional replacement invoice.
- **API:** `POST /api/finances/invoices/{id}/void` with reason; retain the existing issuance endpoint for replacement.
- **Persistence:** New status value and void/audit table or equivalent event are needed. RLS and organization constraints must cover event and replacement links.
- **Authorization/audit:** Owner-only initially. Preserve original issue fields and record the action actor, time, reason, and replacement.
- **Acceptance:** Unpaid invoice can be canceled once; allocation and arrears queries exclude it; repeated void conflicts; partial/paid invoice is rejected; cross-org action is hidden/denied.

### 4. Add credit adjustments for paid or partially paid invoices

- **Rules:** Corrections reduce or increase the amount owed without rewriting the original invoice. A credit exceeding the remaining receivable creates tenant credit/refund treatment, not a negative invoice balance.
- **Domain/application:** Add an immutable credit adjustment linked to the invoice, with positive amount/currency, reason, actor, and effective date. Define whether any overpayment becomes tenant credit or must be refunded before coding.
- **API:** Add an explicit invoice credit/adjustment endpoint; replacement invoice issuance remains separate.
- **Persistence:** Adjustment table/event plus RLS and organization FK constraints. No need to alter the historical invoice amount.
- **Authorization/audit:** Owner-only at first; record the full adjustment and any disposition of existing payment allocations.
- **Acceptance:** Partial-pay correction yields correct net outstanding and preserves payment/allocation history; credit cannot exceed configured policy; all list/balance/arrears read models use effective invoice total.

### 5. Add refund and bank-reversal operations

- **Rules:** Distinguish a chargeback/reversal from a voluntary refund; allow partial amounts and do not void the original receipt. Define settlement timing and tenant-credit behavior.
- **Domain/application:** Add immutable refund/reversal records linked to a payment and, where applicable, allocation/tenant credit. Enforce cumulative refund/reversal bounds and idempotency by external reference.
- **API:** Separate endpoints/actions for refund and external reversal; do not reuse the payment void endpoint.
- **Persistence:** New events and organization-scoped constraints/RLS are required. Keep original payments and allocations intact; derive available credit/receivable impact from events.
- **Authorization/audit:** Explicit refund/reversal capability, initially Owner-only; capture amount, currency, external reference, reason, actor, timestamp, and approval evidence if policy requires it.
- **Acceptance:** Partial and full outflows reconcile exactly once; retry is safe; refunded/charged-back money cannot be allocated again; original receipt and allocations remain traceable; cross-org access is denied.

## Risks and unresolved business decisions

1. **Define “void payment.”** Confirm it means the receipt never occurred. If current usage also includes refunds or bank reversals, historical classifications need a migration/cleanup plan before introducing new event types.
2. **Tenant credit policy.** Decide whether excess payment or an invoice credit is retained as tenant credit, refunded immediately, or both by an explicit process. The domain has no tenant/account-credit ledger today.
3. **Adjustment authority.** Confirm whether Finance can reverse allocations or issue invoice credits, or whether all corrections remain Owner-only. Current Finance permissions cover receipt, allocation, and invoice issue but not void.
4. **Payment retry semantics.** Same reference with a different amount/currency currently returns the original payment; decide the API conflict response and whether references are externally supplied and normalized.
5. **Invoice uniqueness semantics.** The unique key is tied to due date, not an explicit billing period/source key. Confirm manual invoices and monthly generated invoices share this intended identity.
6. **Concurrent corrections.** Keep transaction-local RLS context and concurrency tokens, and add tests for simultaneous void/allocation/reversal races. New append-only records need uniqueness/limits so retries do not duplicate correction events.
7. **Audit completeness.** Decide minimum actor/reason/reference requirements for issuance, allocation, reversal, credit, refund, and cancellation; current `PaymentVoid` is not a general audit system.

## Cross-cutting acceptance criteria

- No financial row or correction can be read or written outside its organization; every query explicitly scopes organization and every transaction establishes `app.organization_id` through the existing interceptor.
- Original invoices, receipts, and allocations remain traceable after correction; correction records identify actor, time, reason, organization, source record, and any replacement.
- Effective outstanding balances reconcile to invoice amount plus/minus approved adjustments less active valid allocations; voided receipts and reversed allocations do not reduce receivable balances.
- Partial/full payments, duplicate retries, correction retries, currency mismatch, over-allocation, and concurrent updates have deterministic outcomes.
- Tests include domain rules, handler authorization, API status/data-disclosure behavior, PostgreSQL RLS/read-write isolation, and concurrency where relevant. No tests should claim invoice voiding or refunds before those workflows exist.
