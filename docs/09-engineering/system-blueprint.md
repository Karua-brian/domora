
# Domora System Blueprint

## Purpose

The System Blueprint defines how the software is organised.

Business rules belong in the Domain.

Application coordinates workflows.

Infrastructure communicates with external systems.

Presentation exposes the application to users.

---

# High-Level Architecture

---

## Presentation

Responsible for

- Web API
- Admin Portal
- Mobile Applications

Contains no business logic.

---

## Application

Responsible for

- Use Cases
- Commands
- Queries
- Workflow Coordination

---

## Domain

Responsible for

- Business Rules
- Entities
- Value Objects
- Domain Events
- Invariants

This is the heart of Domora.

---

## Infrastructure

Responsible for

- PostgreSQL
- Email
- SMS
- Storage
- Payment Gateways
- External APIs

Infrastructure depends on the Domain.

The Domain never depends on Infrastructure.

---

# Architectural Principles

1. Business First
2. Domain Driven
3. Event Driven
4. Modular by Business Capability
5. AI assists but never owns business decisions