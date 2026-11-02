# ADR 008: Relational schema and delete behaviour

- Status: Accepted
- Date: 2026-11-02

## Context

The platform needs a durable, queryable system of record for branches, stock, sales, payments, shipments, money orders, identity verifications and audit history. Each table has a different delete risk.

## Decision

Normalized third-normal-form schema with explicit keys, alternate keys, check constraints and audit columns. Two deliberate denormalizations: transaction-line prices are snapshots, and transaction totals are stored (see DESIGN.md section 3).

Delete behaviour, per table:
- **Restrict** on every foreign key to a master or history table (branches, staff, roles, products, zones, transactions, payments, shipments, money orders, identity verifications, stock adjustments). History is corrected by status change, not deletion.
- **Cascade** only from `CounterTransactions` to `TransactionLines`. Lines are part of one aggregate and have no meaning alone.
- **No foreign keys** on `AuditEntries`. Audit rows outlive the rows they describe.

The full table is in DESIGN.md section 4.

## Consequences

- Deleting a branch, zone or product that has history fails, which is the intended protection.
- Cleaning up test data needs the test database to be dropped, not rows deleted from development data.
- Restrict means an administrator who wants to remove a branch must first deal with its history. That is a deliberate operational step.