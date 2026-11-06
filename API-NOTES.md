# API Notes – ApexPost Logistics API

Base URL: `https://localhost:7xxx` (development). All endpoints return `ProblemDetails` on errors.

| Verb | Route | Purpose | Success | Common errors |
|---|---|---|---|---|
| GET | `/api/shipments` | Paged, filtered list (`page`, `pageSize` ≤ 50, `status`, `city`) | 200 | 400 bad page size |
| GET | `/api/shipments/{trackingNumber}` | One shipment | 200 | 404 unknown tracking number |
| POST | `/api/shipments` | Create a shipment | 201 + `Location` header | 400 validation, 409 duplicate tracking number |
| PUT | `/api/shipments/{trackingNumber}` | Replace editable details (weight, price) | 200 | 400, 404 |
| PATCH | `/api/shipments/{trackingNumber}/status` | Change status only | 200 | 400, 404, 409 invalid transition |
| DELETE | `/api/shipments/{trackingNumber}` | Delete; allowed only while status is `Created` | 204 | 404, 409 wrong status |
| POST | `/api/shipments/{trackingNumber}/scans` | Bulk upload of offline scans | 200 `{accepted, duplicatesSkipped, rejected}` | 400 validation, 404 |
| GET | `/api/shipments/{trackingNumber}/scans` | Scan history | 200 | 404 |

## Validation rules

- Tracking number must match `^APX-\d{8}$`.
- Weight between 0.1 and 70 kg.
- Recipient name, phone, address and city are required; phone must be a valid format.
- Custom rule: a scan's `scannedAtUtc` cannot be more than 5 minutes in the future, and a `Delivered` scan must include a recipient signature in `notes` (see `ShipmentScanDto.Validate`).

## Idempotent scan upload

Each scan carries the Part A scan ID (`scan::{Guid}`). Re-uploading the same ID does not create a duplicate; the response reports `accepted`, `duplicatesSkipped` and `rejected` counts.

## Status transitions

`Created → PickedUp → InTransit → OutForDelivery → Delivered | Failed`. Any other transition returns `409 Conflict` as `ProblemDetails`.
