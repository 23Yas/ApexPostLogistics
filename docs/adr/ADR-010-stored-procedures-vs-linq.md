# ADR 010: Stored procedures versus LINQ

- Status: Accepted
- Date: 2026-11-02

## Context

Some operations need a fixed contract, optional parameters, multi-statement writes or validation that must hold regardless of which caller runs them. "Stored procedures are faster" is not a reason on its own, and this ADR does not claim it.

## Decision

Three operations are procedures. Each has its own reason:

- **`usp_DailyBranchSales`**: a reporting contract with a fixed parameter list (branch and date), used by the report screen and any future integration. Aggregation runs beside the data, and `THROW` rejects a missing branch or date before any work is done.
- **`usp_LowStockReport`**: `@BranchCode` is optional. A single LINQ query would need branching in C#. The procedure uses `OPTION (RECOMPILE)` because the best plan depends on whether the branch is supplied, so a cached plan for one shape would be wrong for the other.
- **`usp_AdjustStock`**: a stock change and its history row must succeed or fail together. The procedure wraps both in `BEGIN TRAN`, `TRY…CATCH` and `XACT_ABORT ON`, so a history row cannot exist without its stock change, even if another caller bypasses the application.

Everything else is LINQ. Counter sales, lookups and screens are easier to test in C# and keep the business rules in one language.

## Consequences

- The application calls procedures only through parameterized interpolated SQL, never concatenated text.
- The application login needs `EXECUTE` on these three procedures only (`sql/12_least_privilege.sql`).
- Procedure logic is tested in SQL Server integration tests, not in unit tests.
- Changing a procedure means re-running `sql/09` in each environment. That is a deliberate deployment step.