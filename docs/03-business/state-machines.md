# Lease State Machine

Draft

↓

Pending Approval

↓

Active

↓

Renewal Pending

↓

Expired

↓

Terminated

Allowed Transitions

Draft → Pending Approval

Pending Approval → Active

Active → Renewal Pending

Renewal Pending → Active

Active → Expired

Active → Terminated

---

# Unit State Machine

Vacant

↓

Reserved

↓

Occupied

↓

Maintenance

↓

Vacant

---

# Invoice State Machine

Draft

↓

Issued

↓

Partially Paid / Overdue 

↓

Paid

↓

Archived

---

# Payment State Machine

Received

↓

Allocated / - Unallocated - if reconciliation fails

↓

Completed