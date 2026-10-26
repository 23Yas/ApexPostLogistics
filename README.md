# ApexPost Counter Platform

ApexPost Logistics is a national postal and retail services operator. This Week 2 solution provides a hybrid layered architecture for branch operations, transaction processing, inventory visibility, shipping logic, and role-based staff access.

## Solution structure

- ApexPost.Core — domain model, value objects, business rules and application services
- ApexPost.Infrastructure — JSON repositories, seed data and simulated integrations
- ApexPost.Web — ASP.NET Core MVC UI and middleware/security pipeline
- ApexPost.Tests — xUnit and WebApplicationFactory coverage

## Branching and collaboration strategy

This repository follows a GitFlow-style workflow to keep development organized across multiple team members.

- `master` is the stable production branch.
- `develop` is the integration branch for all completed features.
- Feature branches are created from `develop` and merged back into `develop` only after review and validation.
- `master` is updated only from a verified `develop` branch.

Current branch map:

```text
master
└── develop
    ├── feature/login
    └── feature/ui-reports
```

Current feature branches in the project:

- `feature/login` — authentication flow, cookie login/logout and role-based access
- `feature/ui-reports` — dashboard, report screens, and presentation-layer polish

Recommended workflow:

1. Start from `develop`
2. Create a feature branch for one delivery area only
3. Commit only the work related to that feature
4. Open a pull request into `develop`
5. Validate the build/tests before merging
6. Merge feature branches into `develop`
7. Merge `develop` into `master` once the release is stable
8. Do not commit directly to `master` or `develop` for feature work

This keeps the team aligned and ensures each developer owns a clear feature slice without mixing unrelated work in a single branch. If more feature work is added later, use the same naming pattern: `feature/<area>` such as `feature/transactions` or `feature/audit-security`.

## Requirement coverage checklist

This project is intentionally scoped to the Week 2 ApexPost specification and avoids speculative or unrelated features.

- Branches and counters: branch records, branch status, counter assignments, and closed-branch rules are implemented.
- Staff roles: counter clerk, branch manager, auditor and administrator are modelled and used in authorization.
- Retail catalogue and stock: product catalog, per-branch stock, low-stock thresholds and reporting are in place.
- Postal and financial services: parcel/letter shipping, service levels, pricing strategy, money orders and identity verification are implemented.
- Mixed basket transaction model: a single transaction can include retail goods and service lines, with totals, tax and status tracking.
- Business rules enforced: insufficient stock, inactive staff, branch mismatch, money-order verification threshold, parcel weight caps, manager-only void/refund, and closed branch restrictions.
- Reports: daily sales, low-stock, shipping volume and money-order/auditor reports are included.
- Persistence and notifications: JSON repositories and simulated events are used behind abstractions, with no database in Week 2.
- Advanced C# 14 usage: records/value objects, pattern matching, delegates, generic repositories, async patterns, ValueTask cache, Task.WhenAll, and SemaphoreSlim are all applied to real problems.
- Security + audit: middleware pipeline, auth policies, API key validation, IP filtering, rate limiting, correlation IDs, audit store, redaction of sensitive values, and Problem Details responses are implemented.
- Tests and docs: the project includes xUnit business-rule checks, MVC integration tests, README, DESIGN.md, ADRs, and an audit sample file.

## Scope boundaries

This Week 2 release intentionally does not introduce a database, ORM, ASP.NET Core Identity, or unrelated business modules. The design is deliberately limited to the ApexPost Week 2 requirement list so the code remains easier to extend into Database/API/Cloud weeks without carrying unnecessary complexity.

## Prerequisites

- .NET 10 SDK
- A local terminal with access to the repo

## Run locally

```bash
dotnet restore

dotnet build

dotnet run --project ApexPost.Web/ApexPost.Web.csproj
```

Then open:

- http://localhost:5000/Account/Login
- or the HTTPS port shown by the ASP.NET host when run in development

## Demo accounts

Use any of the following staff emails and passwords to sign in. These are local demo credentials only and are intentionally seeded as password-hashed values in the local JSON data store.

- Counter clerk: ava.reed@apexpost.local / ApexPostClerk123!
- Branch manager: chloe.moss@apexpost.local / ApexPostManager123!
- Auditor: daniel.price@apexpost.local / ApexPostAuditor123!
- Administrator: ella.north@apexpost.local / ApexPostAdmin123!

## API key setup

For integration endpoints such as `/integration/parcel-status`, set a local secret using user-secrets:

```bash
dotnet user-secrets set "Integration:ApiKey" "local-placeholder-api-key" --project ApexPost.Web/ApexPost.Web.csproj
```

You can also set `Firewall:AllowedIps` in app configuration if you want to restrict the IP allowlist for admin or partner integration routes.

## Implemented features

- Branch dashboard with stock and operations overview
- Product catalogue and stock view
- Counter transaction creation for retail items and shipping service add-ons
- Daily sales and low-stock reports
- Role-based access and branch-level operations
- Cookie authentication, correlation IDs and request timing
- API-key validation, IP restriction, rate limiting and security headers
- Audit action filter and JSON audit log
- Seeded branch, staff, products and transaction sample data

## Sample requests

### Login

```bash
curl -c cookies.txt -X POST "https://localhost:5001/Account/Login" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  --data "email=ava.reed@apexpost.local"
```

### Integration status lookup

```bash
curl -H "X-API-Key: local-placeholder-api-key" "https://localhost:5001/integration/parcel-status"
```

### Dashboard

```bash
curl -b cookies.txt "https://localhost:5001/Home/Dashboard"
```

## Security notes

- Secrets are not committed to source control.
- Cookies use HttpOnly and same-site protections.
- Sensitive values are redacted from audit entries.
- Correlation IDs are echoed in responses for traceability.

## Testing

```bash
dotnet test ApexPost.Tests/ApexPost.Tests.csproj --nologo
```

The suite includes integration tests for security headers, redirects, API-key enforcement, IP blocking and correlation ID propagation.
