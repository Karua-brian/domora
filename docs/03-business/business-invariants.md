# Business Invariants

A business invariant is a rule that must always be true.

---

Organization

An Organization owns every Property beneath it.

Each organization has one Owner through supported application workflows. PostgreSQL restricts persisted roles to Owner, Member, or Finance and enforces at most one Owner. The database does not enforce that an organization always has at least one Owner; supported ownership and membership handlers check and preserve that invariant.

---

Property

Every Property belongs to exactly one Organization.

---

Unit

Every Unit belongs to exactly one Property.

A Unit can have only one Active Lease.

---

Lease

Every Lease references exactly one Unit.

Every Lease references one Tenant.

A Lease Start Date must be before its End Date.

---

Invoice

Every Invoice belongs to a Lease.

Outstanding Balance cannot be negative.

---

Payment

Intended rule: Payment Amount must be greater than zero. The current code accepts zero-value payments; see the [engineering risk register](../09-engineering/risk-register.md).

An allocation cannot exceed the payment's remaining amount or the invoice's outstanding amount. A payment may exceed current invoices; the remainder is retained as unallocated credit. The current payment model has no separate advance designation.

Voiding preserves the payment and allocation rows, records a separate void event with actor, timestamp, and reason, and excludes the voided payment's allocations from effective invoice settlement totals.

---

Audit

Payment voids have a durable actor, timestamp, and reason record. Audit coverage is not uniform across membership changes and other business actions; see the [engineering risk register](../09-engineering/risk-register.md).
