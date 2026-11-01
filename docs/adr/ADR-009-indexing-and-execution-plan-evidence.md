# ADR 009: Indexing and execution-plan evidence

- Status: Accepted
- Date: 2026-11-01

## Context

Branch reports, low-stock checks and transaction lookups are the dominant database read patterns. The database must avoid broad scans without creating excessive write overhead.

## Decision

Use a narrow set of composite indexes to support the most common access patterns, especially branch/date and branch/product filters.

The main strategy is:

- `(BranchCode, CreatedAtUtc)` for daily sales and money-order reporting
- `(BranchCode, ProductSku)` for branch stock lookups
- service and transaction-key indexes for join-heavy transaction queries

The index design is intentionally conservative, because write amplification is real and the app is not a large warehouse workload.

## Consequences

- Better real-world reporting performance for branch and stock workloads
- Lower risk of unbounded scans on the dominant access paths
- More deliberate trade-offs around write cost and storage
