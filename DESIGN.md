# ApexPost design and architecture

## Architecture overview

```text
+---------------------------+
| ApexPost.Web              |
| MVC controllers, views    |
| filters, middleware       |
| auth, routes, policies    |
+------------+--------------+
             |
             v
+---------------------------+
| ApexPost.Core              |
| Domain entities            |
| value objects              |
| application services       |
| business rules and pricing|
+------------+--------------+
             |
             v
+---------------------------+
| ApexPost.Infrastructure    |
| EF Core DbContext          |
| repositories + unit of work|
| migrations + seed data    |
+---------------------------+
```

The project keeps the same top-level architecture as the earlier Week 2 app, but the data layer is now relational and SQL Server-backed through EF Core 10.

## Domain model summary

- Branch: branch identity, active state and operating context
- Staff: staff member with role, home branch and status
- Product: catalogue item with unit price and tax category
- BranchStock: stock position per branch and product with quantity constraints
- CounterTransaction: retail/service transaction with totals and payment state
- TransactionLine: line item details, quantity and pricing
- MoneyOrder: financial service with identity verification requirements
- IdentityVerification: masked verification metadata for audits and compliance
- AuditEntry: action-level audit row for correlation, user and branch context

## Relational schema summary

The schema is designed for a normalized and query-friendly relational model.

- Branches: `Id`, `Code`, `Name`, `Region`, `OpeningHours`, `IsActive`
- Staff: `Id`, `Name`, `Email`, `Role`, `HomeBranch`, `IsActive`
- Products: `Id`, `Sku`, `Name`, `Category`, `UnitPrice`, `TaxCategory`
- BranchStock: `Id`, `BranchCode`, `ProductSku`, `QuantityOnHand`, `ReorderThreshold`, `RowVersion`
- CounterTransactions: `Id`, `BranchCode`, `CounterId`, `ClerkId`, `Status`, `Total`, `RowVersion`
- TransactionLines: `Id`, `CounterTransactionId`, `ProductSku`, `ServiceCode`, `Quantity`, `UnitPrice`
- MoneyOrders: `Id`, `TransactionId`, `BranchCode`, `Amount`, `Fee`, `Status`
- IdentityVerifications: `Id`, `StaffId`, `TransactionId`, `MaskedIdentifier`, `IsVerified`
- AuditEntries: `Id`, `StaffId`, `Role`, `BranchCode`, `Controller`, `Action`, `CorrelationId`, `ResultStatus`, `DurationMs`, `RecordedAtUtc`

## ER diagram

```text
Branches 1---* Staff
Branches 1---* BranchStock
Products 1---* BranchStock
Branches 1---* CounterTransactions
CounterTransactions 1---* TransactionLines
CounterTransactions 1---1 MoneyOrders
CounterTransactions 1---* IdentityVerifications
Staff 1---* IdentityVerifications
CounterTransactions 1---* AuditEntries
```

Key constraints:

- Branch code is unique
- Product SKU is unique
- Staff email is unique
- Branch stock is unique per branch and product
- Quantity, amount and status values are constrained with CHECKs
- `BranchStock` and `CounterTransactions` have rowversion columns for concurrency

## Middleware pipeline order

The application registers middleware in this order:

1. Exception handling
2. Correlation ID generation and propagation
3. Request timing/logging
4. Maintenance-mode guard
5. Security headers
6. HTTPS redirection
7. Routing
8. Rate limiting
9. API-key validation for integration endpoints
10. IP allowlist checks for admin/integration endpoints
11. Authentication
12. Authorization

This keeps cross-cutting concerns outside the controller code while preserving the Week 2 behavior for security and traceability.

## Filters versus middleware

| Concern | Type | Why |
| --- | --- | --- |
| Request tracing | Middleware | Applies to every request before controller code |
| API-key enforcement | Middleware | Uses route metadata before MVC action execution |
| Security headers | Middleware | Must run at the boundary before response is written |
| Audit log for actions | Action filter | Requires controller/action metadata and user identity |
| Model validation | Action filter | Tied to MVC model binding and result handling |
| Performance observation | Action filter | Collects action timing and route metadata |

## ADRs

The project keeps the earlier Week 2 ADRs and adds the Week 3 SQL Server and EF Core ADRs in `docs/adr`:

- ADR-001-hybrid-layered-architecture.md
- ADR-002-json-persistence-for-week-2.md
- ADR-003-authentication-and-role-policy.md
- ADR-004-middleware-order-and-security.md
- ADR-005-audit-and-correlation-tracing.md
- ADR-006-value-objects-and-pricing-strategy.md
- ADR-007-sql-server-and-ef-core.md
- ADR-008-relational-schema-and-delete-behaviour.md
- ADR-009-indexing-and-execution-plan-evidence.md
- ADR-010-stored-procedures-vs-linq.md
- ADR-011-repository-and-unit-of-work-boundaries.md
- ADR-012-domain-vs-persistence-model-separation.md
- ADR-013-concurrency-strategy-for-stock-and-transactions.md
- ADR-014-audit-store-and-unit-of-work.md

## SOLID mapping

- Single responsibility: services own a single business concern such as pricing, staff flow or report generation
- Open/closed: tax, shipping and stock rules can be extended without changing the public interfaces
- Liskov substitution: repository implementations can change without altering business logic consumers
- Interface segregation: the app exposes focused repository contracts instead of broad persistence contracts
- Dependency inversion: Web depends on interfaces and application services instead of concrete infrastructure implementations

## Requirement traceability

| Requirement area | Coverage |
| --- | --- |
| Relational schema | EF Core model and migration scripts define the core relational schema |
| SQL reporting | Reports under `sql/` cover daily sales, low-stock, region totals, shipping and money-order queries |
| Stored procedures | Procedure scripts exist for daily sales, low-stock reporting and stock adjustment |
| EF Core persistence | DbContext, repository layer and UnitOfWork are implemented in Infrastructure |
| Concurrency strategy | Rowversion and transactional patterns are part of the schema and repository design |
| Documentation and ADRs | README, DESIGN.md and ADRs are in place and updated to the SQL Server direction |
| Security and audit | Middleware and filters remain in place and continue to protect the app |

## Security and audit posture

### Threats and mitigations

| Threat | Mitigation |
| --- | --- |
| Brute-force login attempts | Request throttling and lockout logic |
| Missing or invalid API keys | API-key validation middleware and 401 responses |
| IP spoofing | Forwarded header restrictions and allowlist checks |
| Traceability gaps | Correlation ID generation and pass-through |
| Sensitive data leakage | Audit redaction and masked identity handling |
| Clickjacking and XSS | CSP and frame restrictions |
| Insecure transport | HTTPS redirection and HSTS in non-development |

### Audit model

Audit entries capture:

- staff identity and role
- branch code
- controller and action
- correlation ID
- result status and duration
- request metadata and route context
- optional transaction and void reason context

Sensitive fields are deliberately excluded or redacted before being stored.

## Notes on implementation

The Week 3 work keeps the existing business logic and middleware flow intact while replacing the persistence implementation behind the same contracts. The result is a cleaner path for later production-grade SQL Server deployment without forcing a wide rewrite of the application layer.