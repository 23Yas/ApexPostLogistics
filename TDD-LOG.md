# TDD Log – Week 5

## Requirement

> A courier can upload at most 200 scans per batch. Larger batches must be rejected
> with a 400-class error before any scan is persisted.

Chosen because it is a real business rule that did not exist, is small enough to
implement test-first in one cycle, and protects the API from oversized offline-sync
payloads.

## Red → Green → Refactor

Commits on `week5-testing`:

| Commit | Label | Content |
|---|---|---|
| `8a0cdb7` | `RED:` | `UploadScansAsync_MoreThan200Scans_ThrowsBadRequest` — fails with "No exception was thrown" |
| `e3b1543` | `GREEN:` | `MaxScansPerBatch = 200` constant + guard in `UploadScansAsync` throwing `BadRequestException` |
| `eef2d35` | `REFACTOR:` | Extracted `EnsureBatchSize` private guard; added `Exactly200Scans_DoesNotThrow` boundary test |

## First failing test

```csharp
var scans = Enumerable.Range(0, 201).Select(_ => NewScan(ScanType.InTransit)).ToList();
await Assert.ThrowsAsync<BadRequestException>(() => service.UploadScansAsync(Tracking, scans));
```

## Minimal implementation

```csharp
public const int MaxScansPerBatch = 200;
if (scans.Count > MaxScansPerBatch)
    throw new BadRequestException($"A scan batch may contain at most {MaxScansPerBatch} scans");
```

Placed before the shipment lookup so an oversized batch is rejected cheaply.

## What was refactored

- Guard extracted into `EnsureBatchSize` so `UploadScansAsync` reads as a pipeline.
- Boundary test added: exactly 200 scans must NOT throw (off-by-one protection).

## Testability changes to production code

None required — `ShipmentService` already took `IShipmentRepository` via constructor
injection (done in the Week 4 refactor), which is what made the service unit-testable
with Moq. The domain exception `BadRequestException` (also Week 4) gives the test a
precise assertion target instead of a boolean/status-code check.
