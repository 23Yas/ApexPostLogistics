# Versioning – ApexPost Logistics API

## Strategy

URL-segment versioning: `/api/v{version}/shipments`. Both versions share the same `ShipmentService` and EF Core data layer; only DTOs and mapping differ.

## Changes between v1 and v2

| Change | Breaking? | Why |
|---|---|---|
| v1 route `/api/v1/shipments` | — | Baseline contract from Part B |
| v1 marked `Deprecated = true` | Non-breaking | Signals clients to migrate; v1 still served |
| v2 groups recipient fields into `recipient { name, phone, address, city }` | **Breaking** | Response shape changed; old clients expecting flat fields would break |
| v2 adds `latestScan { scanType, location, scannedAtUtc }` | Non-breaking addition | New field, old clients ignore it |
| v2 returns `status` as string enum | **Breaking** | Enum serialisation changed; old clients may expect numeric |

## Deprecation and sunset plan for v1

- v1 is deprecated as of this release and responses include `api-supported-versions` / `api-deprecated-versions` headers added by response middleware.
- Sunset: v1 will be removed 6 months after v2 GA. During the window v1 operations are marked `deprecated: true` in the OpenAPI document and shown as "v1 (deprecated)" in Swagger UI.
- Contract snapshots are stored in `/contracts/v1.json` and `/contracts/v2.json`.

## OpenAPI documents

`/openapi/v1.json` and `/openapi/v2.json` are generated as named OpenAPI documents filtered to each `/api/v{n}` route group; Swagger UI shows both, labelled "v1 (deprecated)" and "v2".
