# ADR 010: Stored procedures versus LINQ queries

- Status: Accepted
- Date: 2026-11-01

## Context

Some operations are naturally centralised and must be validated in one place: daily sales reporting, low stock checks and stock adjustments. The app also needs database-side transactional control for stock movement.

## Decision

Use stored procedures for workloads that are operationally sensitive and centrally governed, especially when they need parameter validation, transactional control and a shared SQL contract. Use EF Core LINQ for everyday object access and read projections when the code is more naturally expressed in C#.

This means:

- `usp_DailyBranchSales` is a procedure because the reporting contract is database-owned and parameter-driven
- `usp_LowStockReport` is a procedure because it is a branch-level operational report with optional parameters
- `usp_AdjustStock` is a procedure because stock mutation is a transactional business event, not a simple update

## Consequences

- Consistent validation and security boundaries in SQL Server
- Explicit, repeatable transactional behaviour for stock changes
- Clear separation between operational SQL and application-level LINQ compositions
