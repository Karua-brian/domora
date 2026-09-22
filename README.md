# Domora

> The Operating System for Property Businesses.

---

## Overview

Domora is a modern platform designed to help property businesses operate with trust, intelligence, and automation.

Rather than being just another property management system, Domora aims to become the operating system that powers the daily operations of rental businesses.

From organizations and properties to leases, invoices, payments, reporting, and AI-powered insights, Domora provides a single platform where every business operation is connected.

---

## Vision

Build the operating system for property businesses.

---

## Mission

Help property businesses operate with complete trust, intelligence, and automation.

---

## Core Principles

- Business before Technology
- Truth over Convenience
- One Source of Truth
- History Never Disappears
- Humans Decide, AI Assists
- Build for the Long Term

---

## Core Capabilities

- Organization Management
- Property Management
- Unit Management
- Tenant Management
- Lease Management
- Invoice Management
- Payments
- Maintenance
- Operational Intelligence

## Documentation

The documentation lives inside the `docs/` directory.

It explains:

- Why Domora exists
- How the business works
- The architecture
- Engineering decisions
- Product roadmap

Documentation is written before implementation decisions are made.

---

## Current Stage

### Phase 1.0: Product Design & System Architecture

Domora is currently in the foundational implementation stage.

The business model and core rental workflows are being translated into a reliable domain model and infrastructure.

Current foundations include:

- Organization-based multi-tenancy
- Property and unit management
- Lease management
- Invoice and payment foundations
- Database transactions
- Optimistic concurrency control
- PostgreSQL Row-Level Security
- Organization-scoped database transactions
- Automated infrastructure tests
- Cross-organization isolation tests

The current objective is to establish the business rules, security boundaries, data integrity, and architectural foundations before expanding into higher-level product capabilities.

Implementation follows the principle:

> **Mindset first, then code.**

The system is built around the real workflows and decisions of property businesses rather than around technology for its own sake.

---

## Engineering Direction

Domora is built with:

- C# / .NET
- ASP.NET Core
- Entity Framework Core
- PostgreSQL

The architecture separates:

- Domain
- Application
- Infrastructure
- API

Security and data integrity are treated as system properties rather than responsibilities left entirely to application code.

For example, organization isolation is enforced at the PostgreSQL layer using Row-Level Security in addition to application-level organization context.

---

## Current Architectural Focus

The current engineering focus is establishing trustworthy foundations for a multi-tenant rental platform.

The system is being developed around several core invariants:

- An organization can only access its own data.
- Cross-organization writes are rejected.
- Concurrent modifications must not silently overwrite each other.
- Financial records must remain traceable.
- Business state changes must follow explicit domain rules.
- Database transactions must preserve business consistency.

These foundations will support the higher-level rental workflows that follow.

---

## Long-Term Direction

Domora aims to evolve from property management software into an operating system for property businesses.

The long-term platform will connect:

**Properties → Units → Tenants → Leases → Invoices → Payments → Operations → Intelligence**

The goal is not simply to store property information.

The goal is to help a property business understand what is happening, protect its money, coordinate its operations, and make better decisions.

---