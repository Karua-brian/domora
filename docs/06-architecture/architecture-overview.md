# Architecture Overview

Domora uses a four-project .NET solution. Domain contains entities, value objects, and state rules. Application handlers coordinate use cases through persistence, context, and authorization interfaces. Infrastructure implements EF Core repositories, transaction handling, PostgreSQL policies, and migrations. API configures authentication, organization access, dependency injection, and HTTP endpoints. Dependencies point inward; Domain remains independent of frameworks.

## Tenant and authorization boundary

The authenticated user identity comes from the JWT subject claim. Requests that need organization scope supply `X-Organization-Id`; `OrganizationAuthorizationMiddleware` verifies persisted membership and sets the organization context. Financial command handlers independently check persisted organization roles (Owner or Finance according to capability), so financial authorization is not enforced only by HTTP middleware.

Organization-scoped database work runs in explicit transactions. `OrganizationTransactionInterceptor` sets transaction-local PostgreSQL `app.organization_id`. RLS policies filter or reject writes on protected property, unit, lease, invoice, payment, allocation, and payment-void data. Composite foreign keys bind allocations and void records to same-organization payment/invoice records. Membership rows are queried for authorization and are not covered by this same RLS policy set.

Membership roles have a PostgreSQL check constraint and a partial unique Owner index. The index enforces at most one Owner; exactly one is maintained by supported registration and membership handlers using the shared organization-row lock and ownership guard. It is not a universal database assertion against arbitrary direct SQL.

This is an implementation snapshot, not a claim of complete financial read/reporting or audit coverage. See the [engineering risk register](../09-engineering/risk-register.md).
