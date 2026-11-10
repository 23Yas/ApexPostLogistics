# API Notes – ApexPost Logistics API

Base URL: `https://localhost:7145` (development; `http://localhost:5254` also bound). All errors return RFC 7807 `ProblemDetails` with `application/problem+json`. Interactive docs: `/swagger` (v1 and v2 documents).

Versioning is URL-segment based (`/api/v1/...`, `/api/v2/...`). v1 is deprecated: responses carry `api-deprecated-versions: 1.0` and `deprecated: true` in the OpenAPI document. v1 returns flat shipment fields; v2 groups recipient details into a `recipient` object and adds `latestScan`. Internal fields (`counterTransactionId`, `branchCode`, `zoneId`) are never exposed — both versions return DTOs.

## v1 – `/api/v1/shipments` (deprecated, JWT bearer required)

| Verb | Route | Purpose | Success | Common errors |
|---|---|---|---|---|
| GET | `/api/v1/shipments` | Paged, filtered list (`page`, `pageSize` ≤ 50, `status`, `city`) | 200 | 400 bad page size, 401 |
| GET | `/api/v1/shipments/{trackingNumber}` | One shipment | 200 | 401, 404 |
| POST | `/api/v1/shipments` | Create a shipment | 201 + `Location` header | 400 validation, 401, 409 duplicate, 415 |
| PUT | `/api/v1/shipments/{trackingNumber}` | Replace editable details | 200 | 400, 401, 404, 415 |
| PATCH | `/api/v1/shipments/{trackingNumber}/status` | Change status only (Courier) | 200 | 400, 401, 403, 404, 409 invalid transition, 415 |
| DELETE | `/api/v1/shipments/{trackingNumber}` | Delete; only while status is `Created` (Admin) | 204 | 401, 403, 404, 409 |
| POST | `/api/v1/shipments/{trackingNumber}/scans` | Bulk upload of offline scans (Courier) | 200 `{accepted, duplicatesSkipped, rejected}` | 400, 401, 403 wrong courierId, 404, 415 |
| GET | `/api/v1/shipments/{trackingNumber}/scans` | Scan history | 200 | 401, 404 |

## v2 – `/api/v2/shipments` (JWT bearer required)

Same operations and status codes as v1, with the v2 response contract (`recipient` object, `latestScan`, `trackingNumber` naming). Scan uploads still require the caller's `courierId` claim to match every scan's `courierId`.

## v2 partner – `/api/v2/partner/shipments/{trackingNumber}`

Read-only lookup authenticated by the `X-Api-Key` header (keys configured under `PartnerApiKeys`). Returns a minimal view: `trackingReference`, `status`, `createdAtUtc`. Errors: 401 missing/invalid key, 404 unknown tracking number.

## Validation rules

- Tracking number must match `^APX-\d{8}$`.
- Weight between 0.1 and 70 kg; price non-negative.
- Recipient name, phone, address and city are required; phone must be a valid format.
- Custom rule: a scan's `scannedAtUtc` cannot be more than 5 minutes in the future, and a `Delivered` scan must include a recipient signature in `notes` (see `ShipmentScanDto.Validate`).

## Idempotent scan upload

Each scan carries the Part A scan ID (`scan::{Guid}`). Re-uploading the same ID does not create a duplicate; the response reports `accepted`, `duplicatesSkipped` and `rejected` counts.

## Status transitions

`Created → PickedUp → InTransit → OutForDelivery → Delivered | Failed`. Rules live in `ShipmentStatusRules` (application layer); any other transition returns `409 Conflict` as `ProblemDetails`. Only `Created` shipments can be deleted.

## Dev tokens

In Development only, `POST /dev/token` issues a JWT: `{"courierId":"courier-001","role":"Courier"}` (`Courier` default; `Admin` for delete endpoints).
