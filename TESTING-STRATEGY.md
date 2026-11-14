# ApexPost Logistics – Test Strategy (Week 5)

## 1. Risk map

What is most likely to break, or most costly if it breaks:

| # | Area | Why it is high risk |
|---|------|---------------------|
| 1 | Shipment status transitions (`ShipmentStatusRules`, `ShipmentService.UpdateStatusAsync`, scan-driven transitions) | Wrong transitions corrupt the shipment lifecycle; couriers depend on them in the field. |
| 2 | Scan de-duplication (`UploadScansAsync`, scan ID uniqueness) | Duplicate scans double-count deliveries; the offline outbox retries make this path hot. |
| 3 | Tracking-number uniqueness on create | Duplicate tracking numbers break lookups, partner API and client SDK contracts. |
| 4 | JWT role rules (Admin create/update/delete, Courier status/scans, courierId claim match) | Authorization regressions leak or block operations silently. |
| 5 | Outbox sync failure classification (transient vs permanent) | A wrong classification either loses courier scans in dead zones or marks good scans Failed forever. |
| 6 | Typed API client retry/circuit-breaker behaviour (`ApexApiClient`, `ResiliencePipeline`, `ApiShipmentUploader.IsTransient`) | Mobile clients retry aggressively; wrong retry policy hammers the API or stalls sync. |
| 7 | Partner API key validation | Constant-time hash comparison; a plain-string compare is a timing side channel. |
| 8 | ProblemDetails contract (status codes + `application/problem+json`) | Clients (generated SDK, courier app) parse error bodies; a wrong content type breaks error handling everywhere. |

## 2. Test pyramid for ApexPost

```
        BDD / acceptance          ~10%   (ApexPost.Specs - Reqnroll, business scenarios at service level)
       / integration              ~25%   (ApexPost.IntegrationTests - WebApplicationFactory + LocalDB, Couchbase Lite temp DB)
      / unit                      ~65%   (ApexPost.UnitTests - services with Moq, validation, rules, HTTP handler fakes)
```

- **Unit (~65%)**: pure logic with no I/O — status rules, validation attributes, `ShipmentService` business rules against a mocked `IShipmentRepository`, `OutboxService` against fake repo/uploader, `ApiShipmentUploader.IsTransient` classification, `PartnerApiKeyFilter.IsValidKey`, `ApexApiClient` against a fake `HttpMessageHandler`.
- **Integration (~25%)**: things that only fail when wired together — API endpoints end-to-end via `WebApplicationFactory` (JWT, roles, partner key, ProblemDetails, versioning headers), EF Core repositories against a real SQL Server LocalDB test database, Couchbase Lite repository against a real embedded database in a temp directory.
- **BDD (~10%)**: a small number of business-language scenarios that exercise the same service/API surface — creation rules and scan sync — proving the requirements are understood, not just the code.

## 3. Framework choice: xUnit

xUnit is used across the whole solution:

- **Consistency**: all existing tests (Week 2–4) are xUnit; one framework keeps `dotnet test` output, Test Explorer grouping and CI simple.
- **Test isolation**: xUnit creates a new instance of the test class per test — no shared mutable state between tests, which the determinism constraint requires. Setup/teardown maps cleanly to constructors/`IAsyncLifetime`/`IDisposable`.
- **Parameterized testing**: `[Theory]` + `[InlineData]` covers the required rules (tracking format, weight limits, status transitions) without extra attributes.
- **Reqnroll support**: `Reqnroll.xUnit` provides the binding/runtime for Gherkin features inside an xUnit project, so BDD stays in the same framework.
- Team familiarity: the existing suite is xUnit; introducing NUnit only for specs would split conventions for no gain.

One framework across `ApexPost.UnitTests`, `ApexPost.IntegrationTests` and `ApexPost.Specs` (all xUnit; specs add Reqnroll on top).

## 4. Existing tests review

| Test class (old location) | Decision | Reason |
|---|---|---|
| `BusinessRulesTests` | kept → `ApexPost.UnitTests` | Pure domain rules; unchanged. |
| `CheckoutServiceTests` | kept → `ApexPost.UnitTests` | Service logic with fakes; unchanged. |
| `CreateSaleRequestTests` | kept → `ApexPost.UnitTests` | Validation rules; unchanged. |
| `PriceRevisionTests` | kept → `ApexPost.UnitTests` | Pricing rules; unchanged. |
| `ProblemDetailsTests` | kept → `ApexPost.IntegrationTests` | Exercises HTTP pipeline; needs the web host. |
| `QuerySqlSamplesTests` | kept → `ApexPost.UnitTests` | SQL text/parameter assertions; no DB needed. |
| `Fakes` | kept → shared into `ApexPost.UnitTests`/`IntegrationTests` as needed | Test doubles reused by moved tests. |
| `CustomWebApplicationFactory` | kept → `ApexPost.IntegrationTests` | Web host factory; unchanged. |
| `WebIntegrationTests` | kept → `ApexPost.IntegrationTests` | HTTP-level web tests; unchanged. |
| `WebSalesAndVoidTests` | kept → `ApexPost.IntegrationTests` | HTTP-level web tests; unchanged. |
| `DatabaseIntegrationTests` | kept → `ApexPost.IntegrationTests` | EF Core against LocalDB test DBs; unchanged. |
| `OutboxServiceTests` | kept → `ApexPost.UnitTests` | Outbox logic with in-memory fakes; unchanged. |
| `ShipmentStatusRulesTests` | kept → `ApexPost.UnitTests` | Pure rules; unchanged. |
| `PartnerApiKeyFilterTests` | kept → `ApexPost.UnitTests` | Hash comparison logic; unchanged. |
| `CouchbaseScanRepositoryTests` | kept → `ApexPost.IntegrationTests` | Real embedded Couchbase Lite; needs filesystem, not a pure unit test. |
| `ApiWebApplicationFactory` | kept → `ApexPost.IntegrationTests` | API host factory; unchanged. |
| `ApiIntegrationTests` | kept → `ApexPost.IntegrationTests` | End-to-end API tests; unchanged. |

Nothing was deleted; every class moved to the project matching its level. `ApexPost.Tests` is removed from the solution once its files are redistributed.

## 5. Data access strategy

| Technique | Used for | What it does NOT prove |
|---|---|---|
| **Moq** (`IShipmentRepository`, `IScanRepository`, `IShipmentUploader`) | Service-layer unit tests: `ShipmentService`, `OutboxService`, controllers' collaborators | Nothing about SQL, EF mappings, transactions or real DB behaviour — only that the service calls the repository correctly. |
| **SQL Server LocalDB test database** (`ApexPostTest_<guid>` / `ApexPostTestApi_<guid>`, migrated per factory, dropped on dispose) | Repository integration tests and `WebApplicationFactory` API tests | Not production data volume or real network latency; proves mappings/constraints/transactions against real SQL Server, not SQLite's different type/constraint semantics. |
| **Couchbase Lite embedded DB in a temp directory** (`DatabaseConfiguration.Directory`) | `CouchbaseScanRepository` tests | Not multi-device sync or production storage limits — proves document model, indexes and queries only. |
| **Fake `HttpMessageHandler`** | `ApexApiClient` unit tests | Not real TLS, DNS, sockets, Polly pipeline behaviour or server responses — proves status-code/JSON handling only. |

EF Core `DbContext`/`DbSet` is never mocked: a mocked `DbSet` returns whatever the test sets up, so it proves the test's own assumptions, not the query. Repository tests run against real databases instead.

## 6. What we will NOT test, and why

- **MAUI UI, pages, navigation, XAML** — out of scope per the brief; UI is verified manually on device/emulator.
- **Browser/front-end testing** — no web UI surface in scope.
- **Real network calls, emails, production databases** — forbidden by constraints; fakes and test databases only.
- **Polly pipeline internals** — the resilience pipeline is Microsoft's tested library; we test our classification logic (`IsTransient`, retry eligibility) at the boundary, not Polly's retry timing.
- **Swagger UI rendering** — the OpenAPI *document* is asserted in integration tests; the UI is a viewer.
- **EF Core migrations themselves** — exercised implicitly by `Database.MigrateAsync()` in every integration fixture.

## 7. Reflection

**What the strategy caught.** The unit suite caught a real production bug during
Week 5: `new ApiJsonContext()` bypassed `JsonSourceGenerationOptions`, so the typed
client silently used PascalCase while the API speaks camelCase — deserialization
returned empty objects. Only the new `ApexApiClientTests` (fake handler, exact JSON
assertions) surfaced it; no existing test covered the client. The BDD specs also
caught two of *my own* wrong assumptions (admin token on a Courier-only endpoint,
7-digit tracking numbers) — exactly the kind of contract drift acceptance tests
exist for.

**What I would change.** I would have split the test projects *before* writing the
Week 4 tests rather than after — the move was mechanical but cost a cycle. I would
also have written the batch-limit rule as a BDD scenario instead of only a unit
test; one rule, two levels, is cheap insurance.

**Most confidence per minute.** The Moq unit tests on `ShipmentService` gave the
best ratio: ~19 tests in one file, milliseconds each, covering the highest-risk
area (status transitions, de-dup, delete rules). Integration tests are slower
(seconds per scenario, LocalDB migrations) but are the only level that proves JWT,
roles and ProblemDetails end-to-end — worth their cost for the auth surface only.
BDD gave the least new signal per minute here because the API-level scenarios
duplicate what `ApiIntegrationTests` already asserts; keep BDD small and business-
facing, not a second integration suite.
