# ADR 008: Relational schema and delete behaviour

- Status: Accepted
- Date: 2026-11-01

## Context

The Week 3 platform needs a durable, queryable system of record for branches, stock, transactions, money orders and audits. JSON files are not sufficient for normalized relational reporting, concurrency and multi-table integrity.

## Decision

Use a normalized SQL Server schema with explicit foreign keys, unique constraints, check constraints and audit metadata. Deletion is intentionally conservative:

- `BranchStock` rows are not deleted in normal business flow; stock is adjusted in place.
- `TransactionLines` are deleted via cascade with their parent transaction because they are dependent details.
- `AuditEntries` are append-only and retained for traceability.

This avoids accidental history loss while keeping referential integrity intact.

## Consequences

- Better relational consistency and easier reporting
- Safer retention of audit/history records
- More explicit operational care around deletes and historical data
