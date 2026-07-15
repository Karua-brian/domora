# ADR-008: State Driven Domain

# Title

State Driven Domain

# Decision

Domora models businesses as state machines.

# Reason

Businesses evolve through state transitions.

Software should validate transitions instead of simply storing records.

# Consequences

Entities become responsible for protecting valid state changes.