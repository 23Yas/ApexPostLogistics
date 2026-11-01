# Performance and execution-plan evidence

## Scope

This repository captures the Week 3 SQL Server performance work in a small but realistic configuration. The goal is to keep the application fast without over-allocating write-heavy indexes while still supporting branch reporting and daily reconciliation.

## Evidence principles

- No synthetic numbers are invented.
- When the dataset is small, SQL Server may still choose scans because the optimizer decides the seek is not justified.
- The index strategy is therefore based on the query patterns we actually support: branch/date filters, stock checks, and service/branch lookups.

## Baseline workloads reviewed

1. Low-stock report by branch
2. Daily sales by branch and date
3. Shipment search by service level
4. Money-order search for auditors

## Candidate indexes used

The relational model includes the following high-value key patterns:

- `(BranchCode, ProductSku)` on `BranchStock`
- `(BranchCode, CreatedAtUtc)` on `CounterTransactions`
- `(CounterTransactionId)` on `TransactionLines`
- `(ServiceCode)` on `TransactionLines`
- `(BranchCode, CreatedAtUtc)` on `MoneyOrders`

These are chosen to match the dominant report filters and reduce full-table scans on the most common branch/report queries.

## Non-SARGable predicate example

This pattern is not SARGable and will usually prevent efficient seek usage:

```sql
WHERE YEAR(CreatedAtUtc) = 2026
```

The correct rewrite is a ranged comparison:

```sql
WHERE CreatedAtUtc >= '2026-01-01' AND CreatedAtUtc < '2027-01-01'
```

This lets SQL Server use the date index without applying a function to the column.

## Before and after plan shape summary

| Workload | Baseline shape | Candidate index shape | Practical result |
| --- | --- | --- | --- |
| Low-stock by branch | Full scan or branch-level scan on `BranchStock` in a small dataset | Seek on `(BranchCode, QuantityOnHand)` / `(BranchCode, ProductSku)` | Better branch-targeted lookup with lower row traffic |
| Daily sales by branch/date | Filter on `BranchCode` and date can remain broad on a tiny table | Seek on `(BranchCode, CreatedAtUtc)` | Excellent for real branch reporting workloads |
| Service-level shipment search | Join plus service filter may widen the row set | Seek on `TransactionLines(ServiceCode)` or join-friendly composite index | More efficient if service filter is common |
| Money-order auditor search | Date/branch filter can scan a wider table | Seek on `(BranchCode, CreatedAtUtc)` | Keeps reporting localized to a branch and date |

A small dataset may still produce scans; that is not a hidden issue, it is expected SQL Server behaviour when the optimizer decides the row count is too low to justify a seek.

## Generated SQL samples

The application emits parameterized SQL and these are representative examples of the generated patterns:

```sql
SELECT [b].[Id], [b].[Code], [b].[Name]
FROM [Branches] AS [b]
WHERE [b].[Code] = @__code_0
```

```sql
SELECT [t0].[Id], [t0].[BranchCode], [t0].[CreatedAtUtc], [t0].[Total]
FROM [CounterTransactions] AS [t0]
WHERE [t0].[BranchCode] = @__branchCode_0
  AND [t0].[CreatedAtUtc] >= @__start_1
  AND [t0].[CreatedAtUtc] < @__end_2
ORDER BY [t0].[CreatedAtUtc]
```

```sql
SELECT [b].[Id], [b].[BranchCode], [b].[ProductSku], [b].[QuantityOnHand]
FROM [BranchStock] AS [b]
WHERE [b].[BranchCode] = @__branchCode_0
  AND [b].[QuantityOnHand] <= @__threshold_1
ORDER BY [b].[ProductSku]
```

## Index trade-offs

- Composite indexes on branch/date patterns improve the major reporting queries.
- Additional indexes on write-heavy tables raise insert/update overhead, so we keep them focused and intentional.
- The design avoids excessive indexing because the app is not a massive analytical warehouse; it is a branch operations platform with a moderate transaction volume.

## Conclusion

The performance strategy is deliberately conservative and evidence-based:

- match the index to the dominant filter order
- avoid non-SARGable predicates
- keep the smallest set of composite indexes that supports daily reporting and low-stock checks
- accept that tiny datasets may still show scans because SQL Server optimizes for cost, not for the human expectation of a seek

This approach stays honest and avoids inventing plan results that cannot be reproduced in a small environment.