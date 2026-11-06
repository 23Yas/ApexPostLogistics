# Offline Design – ApexPost Courier

## Why save locally first

Couriers work in the field where network coverage is unreliable or absent. If scans were sent directly to the API, every scan made in a dead zone would be lost. Saving each scan to a local embedded database (Couchbase Lite) first guarantees that no scan is ever lost: the courier can record pickups, in-transit and deliveries in airplane mode and the data is persisted on the device. The scan is written with `syncStatus = Pending` and the `OutboxService` later uploads pending scans when connectivity returns, marking them `Synced` or `Failed`.

## Conflict handling

Two conflict strategies were considered:

- **Last-write-wins (client side):** the device that wrote last wins. Simple, but a courier editing offline could silently overwrite a newer server-side status.
- **Server-decides (chosen for the upload path):** the server is authoritative for shipment status transitions. Each scan carries a unique scan ID (`scan::{Guid}`), so re-uploads are idempotent and cannot create duplicates. The server validates transitions (Created → PickedUp → InTransit → OutForDelivery → Delivered/Failed) and returns `409 Conflict` for invalid transitions. Scan *events* are append-only facts (a scan happened at a time and place), so conflicts between scans of the same shipment are resolved by ordering on `scannedAtUtc`, not by overwriting.

For document-level conflicts inside Couchbase Lite itself (e.g. Sync Gateway replication, not used here), Couchbase's built-in revision tree handles concurrent edits without data loss.

## Why an embedded database must not be shared

Couchbase Lite is an embedded, single-process database. Its storage engine uses file-level locks and an in-process change notification system. Sharing one database file between multiple processes (e.g. two app instances, or an app and a background sync daemon) corrupts the database or causes `CouchbaseLiteException`. Each courier device therefore runs exactly one application process owning the `apexpost_courier` database; the database directory lives in the app's private storage.
