# ADR 014: Audit store and unit-of-work interaction

- Status: Accepted (replaces the earlier draft of this ADR)
- Date: 2026-11-02

## Context

Every controller action needs an audit row, including failed attempts. An audit problem must never block a sale, and a rolled-back sale must not take its audit trail with it.

## Decision

`EfAuditStore` writes through its own short-lived `ApexPostDbContext`, with its own `SaveChangesAsync`. It is NOT part of the request's unit of work. Failures are logged at Error level and not rethrown.

## Consequences

- An audit row can exist for an action whose business transaction rolled back. This is intended: the row records the attempt, with its `ResultStatus`.
- If the database is down, the audit row can be lost. That is logged. We accept this for availability; if a regulator later requires atomic audit, revisit with an outbox table.
- No distributed transaction is needed, and business commits stay one `SaveChangesAsync` per use case.
