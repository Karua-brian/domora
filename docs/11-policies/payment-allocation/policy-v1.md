# Payment Allocation 

## Policy v1

- A payment may be larger than an invoice.
- An invoice may never receive more than its outstanding balance.
- A payment may never allocate more than its remaining balance.
- If an invoice becomes fully settled, it is marked Paid.
- Any excess from an active payment remains available for future allocation.
- Voiding a payment preserves its original allocation records and records a separate void event.
- Allocations belonging to a voided payment do not reduce an invoice's effective outstanding balance.
- A voided payment and its remaining credit cannot be allocated or reused.
