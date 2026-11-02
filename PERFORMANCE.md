# Performance and execution-plan evidence

> **Status: method and template only.** No numbers appear in this file until they are measured on your database. Every table below is empty on purpose. The brief says never to invent numbers. Fill each table from a real run, save the `.sqlplan` file next to this document, and keep the statistics output in the same folder.

## 1. Method

1. Load realistic volume: `sql/15_generate_volume.sql` adds 50,000 transactions on top of the 200 seeded ones. Record the row counts before and after.
2. Clear the plan cache and buffers only on a development machine: `DBCC FREEPROCCACHE; DBCC DROPCLEANBUFFERS;`.
3. For each workload in `sql/08_performance_baseline_queries.sql`, run it with **Include Actual Execution Plan** on, and save the plan as `.sqlplan`. Record the `STATISTICS IO` (logical reads) and `STATISTICS TIME` (CPU time, elapsed time) output.
4. Create one candidate index from `sql/13_candidate_indexes.sql` at a time. Run the workload again and record the same numbers.
5. Record the write overhead: time 10,000 `INSERT`s into `CounterTransactions` with and without the index, and the storage with `EXEC sp_spaceused 'CounterTransactions';`.
6. Keep the index only if the workload improves and the write cost is acceptable. Then add it to the EF configuration and create a migration, so the schema in the repository matches the database.

## 2. Workloads

| # | Workload | Query (in `sql/08`) | Filter columns | Candidate index (`sql/13`) |
|---|---|---|---|---|
| W1 | Low-stock report by branch | `BranchStock` where `BranchCode` and `QuantityOnHand <= 5` | BranchCode (equality), QuantityOnHand (range) | `(BranchCode, QuantityOnHand)` INCLUDE `(ProductSku)`. Equality column first, range second. The INCLUDE makes it covering, so no key lookup. |
| W2 | Daily sales by branch and date | `CounterTransactions` where `BranchCode` and a `CreatedAtUtc` range | BranchCode (equality), CreatedAtUtc (range) | `(BranchCode, CreatedAtUtc)` INCLUDE `(Total, Status)`. Covers the daily total without touching the base table. |
| W3 | Shipment search by zone and service level | `Shipments` where `ZoneId` and `ServiceLevel` | ZoneId, ServiceLevel (both equality) | `(ZoneId, ServiceLevel)` INCLUDE `(Price)`. Both equality columns; the order puts the more selective one first, which must be confirmed with the density values. |
| W4 | Money-order search for auditors | `MoneyOrders` where `BranchCode` and a `CreatedAtUtc` range | BranchCode, CreatedAtUtc | `(BranchCode, CreatedAtUtc)` INCLUDE `(Amount, Status)`. The index already exists from the migration, so this measures whether the INCLUDE helps. |

Key order rule used here: equality predicates first, then the range predicate, then the columns used for sorting. A range column placed before an equality column stops the seek from using the equality column.

## 3. Baseline (before any new index)

| Workload | Logical reads | CPU ms | Elapsed ms | Plan shape (seek / scan / lookup) | Plan file |
|---|---|---|---|---|---|
| W1 | _to measure_ | _to measure_ | _to measure_ | _to measure_ | _to save_ |
| W2 | _to measure_ | _to measure_ | _to measure_ | _to measure_ | _to save_ |
| W3 | _to measure_ | _to measure_ | _to measure_ | _to measure_ | _to save_ |
| W4 | _to measure_ | _to measure_ | _to measure_ | _to measure_ | _to save_ |

## 4. After each candidate index

| Workload | Index | Logical reads | CPU ms | Elapsed ms | Plan shape | Insert overhead (10k rows) | Storage (MB) |
|---|---|---|---|---|---|---|---|
| W1 | `IX_BranchStock_LowStock` | _to measure_ | | | | | |
| W2 | `IX_CounterTransactions_BranchCode_CreatedAtUtc` (covering) | _to measure_ | | | | | |
| W3 | `IX_Shipments_ZoneId_ServiceLevel` | _to measure_ | | | | | |
| W4 | `IX_MoneyOrders_BranchCode_CreatedAtUtc` (covering) | _to measure_ | | | | | |

## 5. Non-SARGable predicate

The predicate `WHERE YEAR(CreatedAtUtc) = 2026` applies a function to the column, so an index on `CreatedAtUtc` cannot be used for a seek. `sql/14_sargable_rewrite.sql` gives the baseline and the rewrite as a date range.

| Version | Predicate | Logical reads | CPU ms | Plan shape |
|---|---|---|---|---|
| Before | `YEAR(CreatedAtUtc) = 2026` | _to measure_ | | _to measure_ (expect a scan) |
| After | `CreatedAtUtc >= '2026-01-01' AND CreatedAtUtc < '2027-01-01'` | _to measure_ | | _to measure_ (expect a seek) |

## 6. Estimated versus actual row mismatch

Method: run the W2 query with the statistics as they are, then with `UPDATE STATISTICS dbo.CounterTransactions WITH FULLSCAN` after bulk loading. In the actual plan, compare **Estimated Number of Rows** with **Actual Number of Rows** on each operator.

| Run | Estimated rows | Actual rows | Ratio | Cause | Action |
|---|---|---|---|---|---|
| Before update statistics | _to measure_ | _to measure_ | | Stale statistics after bulk insert (expected) | |
| After update statistics | _to measure_ | _to measure_ | | | |

Write the explanation here once measured: which operator was wrong, why the optimizer chose the plan it did, and what changed after the statistics update.

## 7. Generated SQL samples (`ToQueryString`)

Capture each query with `query.ToQueryString()` in a development build, then paste the output below. These are the three queries the brief asks for.

| Query | Generated SQL |
|---|---|
| Daily sales for a branch and date range | _paste from `ToQueryString()`_ |
| Stock lookup for a sale (`GetByBranchAndSkuAsync`) | _paste_ |
| Low-stock list (`GetLowStockAsync`) | _paste_ |

## 8. Conclusions

Written after measurement only. For each workload: which index was kept, why, and the trade-off in write cost and storage. Record any workload that stayed a scan, and explain why. Small tables can legitimately scan, because the optimizer chooses a scan when a seek would not be cheaper.