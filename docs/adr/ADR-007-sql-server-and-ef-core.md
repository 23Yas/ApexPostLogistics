# ADR 007: SQL Server and EF Core persistence

- Status: Accepted
- Date: 2026-11-01

## Context

The Week 3 requirements require a relational system of record with transactions, stock integrity, reporting and normalized schema design. JSON files are inadequate for concurrency, relational integrity and multi-table reporting at scale.

## Decision

Use SQL Server as the only durable database and EF Core 10 as the persistence and migration layer. The application continues to expose the same repository interfaces and unit-of-work abstraction so the domain logic and web layer remain stable while the storage implementation changes.

## Consequences

- Full relational integrity with foreign keys and constraints
- Better concurrent stock updates and transactional safety
- More explicit migration and schema management
- Slight increase in operational setup complexity compared with JSON files
