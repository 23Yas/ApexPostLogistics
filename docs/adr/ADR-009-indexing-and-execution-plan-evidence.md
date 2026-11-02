# ADR 009: Indexing and execution-plan evidence

- Status: Proposed. Becomes Accepted when the measurements in PERFORMANCE.md are filled in.
- Date: 2026-11-02

## Context

Four workloads dominate reads: the low-stock report, daily sales, shipment search and money-order search for auditors. Every index adds write cost and storage, so each one must be justified by a measurement.

## Decision

Index only what the measurements justify. Candidates and their key-order reasons are in PERFORMANCE.md section 2 and `sql/13_candidate_indexes.sql`. The key-order rule is: equality predicates first, then the range predicate, then sort columns. INCLUDE columns are added only when they make the index covering for a measured workload.

Decision criteria for keeping an index:
1. The workload's logical reads or elapsed time drop by a clear margin on the volume loaded by `sql/15`.
2. The insert overhead measured on 10,000 rows is acceptable for a branch operations system.
3. The index is added to the EF configuration and a migration, so the schema in the repository matches.

## Consequences

- Indexes not kept are dropped, and the rationale is recorded in PERFORMANCE.md.
- A non-SARGable predicate (`YEAR(column)`) is rewritten as a range, because no index can serve it as written.
- Until the measurements are in, this ADR stays Proposed. The existing migration indexes are the current, unmeasured choice.