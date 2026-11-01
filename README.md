# ApexPost Logistics

ApexPost Logistics is a retail and postal operations platform. The current implementation keeps the Week 2 business logic and security model intact while moving the durable data layer to SQL Server and EF Core 10.

## Solution structure

- ApexPost.Core — domain model, value objects, pricing and business rules
- ApexPost.Infrastructure — EF Core DbContext, repository implementations, migration setup and seed data
- ApexPost.Web — ASP.NET Core MVC app, middleware and filters
- ApexPost.Tests — business-rule and MVC integration tests

The project follows a clean dependency direction:

Web -> Application/Interfaces -> Infrastructure -> Domain

## SQL Server setup

The database for this project is named `ApexPostDb` and is intentionally created as a dedicated local database instead of a shared training database.

Create it in SQL Server or LocalDB, then apply migrations:

```bash
dotnet ef database update --project ApexPost.Infrastructure/ApexPost.Infrastructure.csproj --startup-project ApexPost.Web/ApexPost.Web.csproj
```

If you are using a local SQL Server instance, configure the connection string with user-secrets:

```bash
dotnet user-secrets set "ConnectionStrings:ApexPostDb" "Server=(localdb)\\MSSQLLocalDB;Database=ApexPostDb;Trusted_Connection=True;TrustServerCertificate=True;" --project ApexPost.Web/ApexPost.Web.csproj
```

For the partner integration endpoint and policy checks:

```bash
dotnet user-secrets set "Integration:ApiKey" "local-placeholder-api-key" --project ApexPost.Web/ApexPost.Web.csproj
```

Do not commit secrets to source control. The repository must only contain fictional values.

## Run locally

```bash
dotnet restore

dotnet build

dotnet run --project ApexPost.Web/ApexPost.Web.csproj
```

Then open the app in the browser at the ASP.NET Core port shown in the terminal, or use the route directly:

- `/Account/Login`
- `/Home/Dashboard`

## Demo accounts

The seed data contains demo staff accounts for local testing.

- Counter clerk: `ava.reed@apexpost.local` / `ApexPostClerk123!`
- Branch manager: `chloe.moss@apexpost.local` / `ApexPostManager123!`
- Auditor: `daniel.price@apexpost.local` / `ApexPostAuditor123!`
- Administrator: `ella.north@apexpost.local` / `ApexPostAdmin123!`

## Required SQL artifacts

The repo includes the Week 3 database artifacts in the `sql` folder:

- 01_daily_branch_sales.sql
- 02_branches_without_sales_today.sql
- 03_top_products_by_region.sql
- 04_shipping_volume_by_zone_service.sql
- 05_money_orders_and_identity_verifications.sql
- 06_sale_transaction_example.sql
- 07_blocking_demo.sql
- 08_performance_baseline_queries.sql
- 09_stored_procedures.sql
- 10_schema_script.sql
- 11_schema_script_with_indexes.sql

These scripts cover the reporting queries, transaction examples, blocking demonstration, schema generation, and procedure definitions required for the assignment.

## Database behaviour and constraints

The relational model keeps the platform's Week 2 business rules intact while moving to SQL Server. Main design points:

- Branches, counters, staff, products and stock are normalized for relational operations
- `BranchStock` and `CounterTransactions` carry rowversion fields for optimistic concurrency
- Quantity, amount and status rules are enforced with check constraints
- Identity document storage remains masked only; full numbers are never persisted
- Audit data is recorded in a dedicated `AuditEntries` table and can be written without breaking the business transaction

## Security notes

- Secrets are kept in user-secrets and environment settings
- API keys are validated before protected endpoints run
- IP allowlists are enforced for partner/admin routes
- Request timing, correlation IDs and exception handling remain active
- Audit entries redact sensitive values and avoid storing full document identifiers

## Example requests

### Login

```bash
curl -c cookies.txt -X POST "https://localhost:5001/Account/Login" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  --data-urlencode "email=ava.reed@apexpost.local" \
  --data-urlencode "password=ApexPostClerk123!"
```

### Integration status lookup

```bash
curl -H "X-API-Key: local-placeholder-api-key" "https://localhost:5001/integration/parcel-status"
```

### Dashboard

```bash
curl -b cookies.txt "https://localhost:5001/Home/Dashboard"
```

## Validation

```bash
dotnet test ApexPost.Tests/ApexPost.Tests.csproj --nologo
```

The current suite verifies business rules, middleware security, redirection logic, role enforcement and the core platform flow while the SQL-backed persistence layer remains behind the same application contracts.

## Design documentation

The project includes the design and ADR set under `DESIGN.md` and `docs/adr/` with the Week 3 schema, reporting, index and concurrency trade-offs documented alongside the earlier architecture decisions.