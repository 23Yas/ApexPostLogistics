# ApexPost design and architecture

## Architecture overview

```text
+---------------------------+
| ApexPost.Web              |
| MVC controllers, views    |
| filters, middleware      |
| auth + routing            |
+------------+--------------+
			 |
			 v
+---------------------------+
| ApexPost.Core             |
| Domain entities           |
| value objects             |
| application services      |
| pricing and business rules|
+------------+--------------+
			 |
			 v
+---------------------------+
| ApexPost.Infrastructure   |
| JSON repositories         |
| seed data + notifications |
+---------------------------+
```

## Domain model summary

- Branch: physical retail branch with branch code, status and terminals
- Staff: named employee with role, home branch and active state
- Product/StockItem: product catalogue and branch-specific inventory
- CounterTransaction: mixed basket of goods and services with totals, payment and status
- ShippingQuote: parcel/letter service configuration
- MoneyOrder and IdentityVerification: financial and KYC checks

## Middleware pipeline order

The application registers middleware in this order:

1. Exception handling
2. Correlation ID generation and propagation
3. Request timing/logging
4. Maintenance mode guard
5. Security headers
6. HTTPS redirection
7. Routing
8. Rate limiting
9. API-key validation for integration endpoints
10. IP allowlist checks for admin/integration endpoints
11. Authentication
12. Authorization

Why this order matters:

- Exception handling must wrap the app as early as possible to protect all routes.
- Correlation IDs are created before any downstream logic so all logs share the same trace.
- Maintenance mode and security headers protect the app before expensive work is done.
- Request throttling and IP checks protect protected endpoints before auth and routing logic.
- Authentication and authorization run after route resolution so policies can be applied accurately.

## Filters versus middleware

| Concern | Type | Why |
| --- | --- | --- |
| Request tracing | Middleware | Applies to all HTTP requests before controller code | 
| API-key enforcement | Middleware | Requires route metadata and runs before MVC action execution |
| Security headers | Middleware | Must run at the boundary before response is written |
| Audit log for actions | Action filter | Requires controller/action metadata and validated user identity |
| Model validation | Action filter | Tied to MVC model binding and view result handling |
| Slow action warnings | Action filter | Needs action context and execution timing |

## ADRs

The project includes the following ADRs in the `docs/adr` folder:

- ADR-001-hybrid-layered-architecture.md
- ADR-002-json-persistence-for-week-2.md
- ADR-003-authentication-and-role-policy.md
- ADR-004-middleware-order-and-security.md
- ADR-005-audit-and-correlation-tracing.md
- ADR-006-value-objects-and-pricing-strategy.md

## SOLID mapping

- Single responsibility: services are focused on one concern such as pricing, staff operations or stock reporting
- Open/closed: pricing and tax logic are centralized and can be extended without changing the domain model contract
- Liskov substitution: repository abstractions can be replaced by different storage implementations without impacting business logic
- Interface segregation: repository contracts separate branch, staff, product, stock and transaction responsibilities
- Dependency inversion: the web layer depends on service interfaces and application abstractions rather than concrete JSON files

## Security and audit posture

### Threats and mitigations

| Threat | Mitigation |
| --- | --- |
| Brute-force login attempts | Login rate limiting and strict throttling |
| Missing or invalid API keys | API-key middleware with constant-time comparison and 401 responses |
| IP spoofing or unauthorised partner access | IP allowlist middleware with 403 response |
| Request traceability loss | Correlation ID middleware and structured logging |
| Sensitive data leakage | Audit redaction for passwords, document numbers and API secrets |
| Cross-site scripting or clickjacking | CSP, frame-ancestors and X-Frame-Options |
| Mixed content and insecure transport | HTTPS redirect and HSTS in non-development |

### Audit model

The audit log records:

- staff identity and role
- branch code
- controller and action
- correlation ID
- result status and duration
- route values and request metadata

Sensitive fields are intentionally excluded or redacted before they are persisted to the JSON audit store.

## Notes on implementation

This Week 2 release keeps persistence in JSON files behind repository abstractions so the same domain logic can later be lifted to a database with minimal business-layer impact.
