# ADR 011: Repository and Unit of Work boundaries

- Status: Accepted
- Date: 2026-11-01

## Context

The application needs a consistent persistence boundary without leaking database details into the domain or controller layer.

## Decision

Use the repository pattern for aggregate-level persistence and the Unit of Work for transaction coordination. Repositories stage changes but never call `SaveChangesAsync` directly. The unit of work owns the transaction boundary and commit timing.

This keeps the app aligned with the earlier Week 2 architecture and enables EF Core to replace the storage implementation without changing the public application contracts.

## Consequences

- Business logic remains decoupled from storage details
- The same application interfaces can be implemented with either file-based or SQL-backed persistence
- One commit per use case retains domain and transaction clarity
