# ApexPost Logistics

ApexPost is a retail and postal operations platform: counter sales, stock, money orders, identity verification, shipping labels and reports. This branch moves the platform from JSON files to SQL Server with EF Core 10.

## Prerequisites

- .NET 10 SDK
- SQL Server LocalDB (installed with Visual Studio) or SQL Server Express
- SQL Server Management Studio (for the scripts in `sql/`)

## 1. Set the secrets (never commit these)

From the repository root:

```
dotnet user-secrets init --project ApexPost.Web
dotnet user-secrets set "ConnectionStrings:ApexPostDb" "Server=(localdb)\MSSQLLocalDB;Database=ApexPostDb;Trusted_Connection=True;TrustServerCertificate=True;" --project ApexPost.Web
dotnet user-secrets set "Integration:ApiKey" "local-placeholder-api-key" --project ApexPost.Web
```

The app will not start without the connection string. Use fictional values only.

## 2. Create the database and schema

You do not need to create `ApexPostDb` by hand. The migrations create it.

```
dotnet build ApexPostLogistics.slnx
dotnet ef database update --project ApexPost.Infrastructure --startup-project ApexPost.Web
```

To rebuild from scratch (development only):

```
dotnet ef database drop --force --project ApexPost.Infrastructure --startup-project ApexPost.Web
dotnet ef database update --project ApexPost.Infrastructure --startup-project ApexPost.Web
```

To generate the idempotent schema script for review or deployment:

```
dotnet ef migrations script --idempotent --project ApexPost.Infrastructure --startup-project ApexPost.Web -o sql/10_schema_script.sql
```

## 3. Stored procedures

The three procedures are created by the migration `AddStoredProcedures`, so `dotnet ef database update` creates them. `sql/09_stored_procedures.sql` holds the same definitions for SSMS. Running it again is safe (`CREATE OR ALTER`).

## 4. Optional scripts

| Script | Purpose |
|---|---|
| `sql/01` to `sql/05` | Reporting queries |
| `sql/06` | Sale transaction example (T-SQL, transactional) |
| `sql/07` | Blocking demonstration (two SSMS sessions) |
| `sql/08` | Performance baseline (four workloads, with STATISTICS IO and TIME) |
| `sql/13` | Candidate indexes, one per workload (create only after measuring) |
| `sql/14` | Non-SARGable rewrite example |
| `sql/15` | Adds 50,000 historical transactions for realistic volume (run once) |
| `sql/12` | Least-privilege role for the application login (run as administrator) |

## 5. Run the app

```
dotnet run --project ApexPost.Web
```

On startup the app applies pending migrations and seeds the reference and history data once (branches, staff, products, stock, counters, zones, rates, 200 transactions, payments and shipments). Then browse to `/Account/Login`.

## 6. Demo accounts

These are fictional, for local testing only.

| Role | Email | Password |
|---|---|---|
| Counter clerk | `ava.reed@apexpost.local` | `ApexPostClerk123!` |
| Branch manager | `chloe.moss@apexpost.local` | `ApexPostManager123!` |
| Auditor | `daniel.price@apexpost.local` | `ApexPostAuditor123!` |
| Administrator | `ella.north@apexpost.local` | `ApexPostAdmin123!` |

## 7. Run the tests

```
dotnet test ApexPost.Tests
```

The tests never use `ApexPostDb`. Each test class creates a database named `ApexPostTest_<guid>`, applies the migrations and drops it when finished. LocalDB must be running.

## 8. Project layout

| Project | Contents |
|---|---|
| `ApexPost.Core` | Domain entities, value objects, application services, repository interfaces |
| `ApexPost.Infrastructure` | `ApexPostDbContext`, entity configurations, repositories, unit of work, migrations, seeding |
| `ApexPost.Web` | MVC controllers, views, middleware, filters, `Program.cs` (composition root) |
| `ApexPost.Tests` | Unit tests (no database), SQL Server integration tests, web tests |
| `sql/` | Reporting queries, stored procedures, performance scripts, least-privilege script |
| `docs/adr/` | Architecture decision records |

See `DESIGN.md` for the entity-relationship diagram, delete behaviour and the requirement status, and `PERFORMANCE.md` for the index evidence.

## Week 4 projects

| Project | Contents |
|---|---|
| `ApexPost.Courier.Offline` | Couchbase Lite offline scan store (Part A) |
| `ApexPost.Courier.Offline.Console` | Console test harness for the offline store |
| `ApexPost.Api` | REST API v1/v2 with Swagger UI (Parts B, C, D) |
| `ApexPost.Client` | NSwag-generated C# client from `contracts/v2.json` |
| `ApexPost.Client.Console` | Console consumer for the generated client |
| `ApexPost.Courier` | .NET MAUI courier app with offline sync (Part D) |
| `contracts/` | OpenAPI contract snapshots `v1.json`, `v2.json` |
| `screenshots/` | Part A–D evidence: query results, listener log, Swagger responses, demo video |

Week 4 work was developed on branches `week4-partA`, `week4-partB`, `week4-partC` and `week4-partD`, each merged into `master`.

### Regenerating contracts and the client

Run the API (`dotnet run --project ApexPost.Api`, https profile on port 7145) and save the OpenAPI documents:

```
curl -k https://localhost:7145/openapi/v1.json -o contracts/v1.json
curl -k https://localhost:7145/openapi/v2.json -o contracts/v2.json
```

Then regenerate the C# client from the v2 contract with NSwag (installed as a global tool):

```
nswag run nswag.json
```

This reads `contracts/v2.json` (configured in `nswag.json`) and rewrites `ApexPost.Client/Generated/ApexPostClient.g.cs`.

### OpenAPI vs. Swagger UI

The **OpenAPI specification** is the machine-readable contract (`/openapi/v1.json`, `/openapi/v2.json`) — it describes endpoints, request/response schemas and status codes, and is what SDK generators consume. The **built-in .NET generator** (`AddOpenApi()` / `MapOpenApi()`) produces that document automatically from the code (XML comments, attributes). **Swagger UI** is only the interactive documentation *viewer* at `/swagger` that renders the OpenAPI document and offers "Try it out". Swagger UI adds no specification of its own — it just displays what the generator produced. Only one generator runs: we use the built-in .NET OpenAPI generator; Swashbuckle is used for Swagger UI hosting only, not a second spec generator.