# ADR 014: Audit store and unit-of-work interaction

- Status: Accepted
- Date: 2026-11-01

## Context

Audit logs are important for traceability, but they must not silently break the business transaction. A failed stock or transaction write should not leave an inconsistent or misleading audit trail.

## Decision

The audit store writes through the same EF DbContext and participates in the same request-scoped unit-of-work boundary. This makes the write path consistent and prevents the audit row from being persisted independently from the business event.

The audit entry is treated as an append-only operational record, not as a stand-alone business action.

## Consequences

- Better consistency between business writes and operational traceability
- Less risk of audit rows describing transactions that never persisted
- Clearer operational review and incident investigation
