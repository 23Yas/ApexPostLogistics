# ADR 013: Concurrency strategy for stock and transactions

- Status: Accepted
- Date: 2026-11-01

## Context

Stock integrity is business-critical. Multiple clerks can attempt to sell the same last unit within a small time window.

## Decision

Use optimistic concurrency with rowversion columns on the stock and transaction tables, plus transactional boundaries around each mutation. A sale modifies stock and transaction rows in the same unit of work. If the rowversion changes during update, the app surfaces a concurrency exception and returns a clear 409-style response.

This is preferable to a naive last-write-wins approach and keeps the transaction safe without locking the table for the entire application.

## Consequences

- Better safety under concurrent sales
- Clear user-facing concurrency error handling
- Consistent stock integrity for branch operations
