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

Domora is in active foundational implementation. The repository currently contains working organization registration and membership management, property and unit registration, lease registration and termination, invoice issuance and monthly invoice generation, payment receipt/allocation, payment voiding, and role-based financial authorization. This implementation snapshot does not imply production readiness; see the [engineering risk register](docs/09-engineering/risk-register.md) for known gaps.

The implemented organization model uses Owner, Finance, and Member roles. PostgreSQL enforces supported role values and at most one Owner per organization. Supported application workflows create the Owner and preserve one Owner during ownership and membership mutations.

Implementation follows the principle:

> **Mindset first, then code.**

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

## Local Development and Tests

Use the guarded PostgreSQL test workflow for database-backed tests:

```sh
./scripts/test-with-db.sh
```

The script loads local `.env.test.local`, verifies the configured `domora_test` connection, and runs solution tests using the application role. Do not pass development or production connection strings to the test runner. Rebuilding the test database is a separate destructive operation in `scripts/rebuild-test-db.sh`; it uses the dedicated local test administrator and database identity/session guards. It is not part of routine test execution.

For tenant boundaries and project responsibilities, see [Architecture Overview](docs/06-architecture/architecture-overview.md). See [Business Invariants](docs/03-business/business-invariants.md) alongside the implementation risk register; documentation describes current behavior and known limits.

### Test the API with Swagger UI

Start the API with the Development launch profile:

```sh
dotnet run --project src/Domora.API --launch-profile https
```

Open Swagger UI at <https://localhost:7205/swagger>. The OpenAPI JSON is at <https://localhost:7205/swagger/v1/swagger.json>. The HTTP profile is available at <http://localhost:5278/swagger>. Swagger is enabled only when `ASPNETCORE_ENVIRONMENT=Development`; the routes are not registered in Production.

The API does not create or migrate the database at startup. Configure a local development database and apply the existing migrations through the approved development workflow before trying database-backed requests. Do not use production credentials for local testing.

1. In Swagger, call `POST /auth/register` with a development email and password, then `POST /auth/login` with the same values. Copy `accessToken` from the login response.
2. Select **Authorize**, paste the token only (without typing the `Bearer ` prefix), and authorize. The Swagger security scheme sends it as a bearer JWT. `POST /auth/register` and `POST /auth/login` remain anonymous.
3. Call `POST /api/Organizations` to create an organization. Copy its returned `id`.
4. For an endpoint marked with `X-Organization-Id`, enter that organization ID in the header field shown by Swagger. For example, call `POST /api/properties` with `{ "name": "Central House" }` to create a property in that organization.
5. Continue with the unit and lease registration endpoints, then invoice/payment operations. Payment and invoice write operations require a persisted Owner or Finance membership as defined by the existing authorization rules; voiding is Owner-only.

The current API has no payment or invoice history read endpoints, so Swagger lists the implemented writes and the existing property read rather than implying unsupported finance queries.

---

## Long-Term Direction

Domora aims to evolve from property management software into an operating system for property businesses.

The long-term platform will connect:

**Properties → Units → Tenants → Leases → Invoices → Payments → Operations → Intelligence**

The goal is not simply to store property information.

The goal is to help a property business understand what is happening, protect its money, coordinate its operations, and make better decisions.

---
