# Business Invariants

A business invariant is a rule that must always be true.

---

Organization

An Organization owns every Property beneath it.

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

Payment Amount must be greater than zero.

Payments cannot exceed the outstanding balance unless explicitly marked as an advance.

---

Audit

Business records are never silently deleted.

Every significant action must be traceable.