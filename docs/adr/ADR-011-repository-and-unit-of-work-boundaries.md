# ADR 011: Repository and unit-of-work boundaries

- Status: Accepted
- Date: 2026-11-02

## Context

Writes must be consistent and committed once per use case. Reads for reports must be fast and must not load entities they do not need.

## Decision

- **Writes** go through aggregate repositories (`IStockRepository`, `ICounterTransactionRepository`, `IPaymentRepository`, `IIdentityVerificationRepository`, `IStaffRepository`, `IBranchRepository`, `IProductRepository`). Repositories stage changes. `IUnitOfWork.SaveChangesAsync` is the only commit, once per use case.
- **Reads for screens and reports** may use `AsNoTracking()` projections, directly or through a query service. Read-only projections are where a DbContext is used directly, because they cannot change state.
- **No repository returns `IQueryable`.** Callers cannot compose queries against the database from outside the persistence layer.
- **Stock reads on a write path are tracked** (`EfStockRepository.GetByBranchAndSkuAsync`), so the rowversion is loaded and the concurrency check works.
- **The audit store** uses its own context (ADR-014), so it sits outside this boundary on purpose.

## Consequences

- Reports can be written as projections without new repository methods for each report.
- The generic `IRepository<T>` still exposes `GetAll` and `Delete`. These should be narrowed to aggregate-specific methods in the next round.
- Some controllers still call services that read through repositories directly. Those reads are acceptable only when they are AsNoTracking projections.