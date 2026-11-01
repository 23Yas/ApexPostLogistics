# ADR 012: Domain and persistence model separation

- Status: Accepted
- Date: 2026-11-01

## Context

The domain model contains verified business invariants and value objects. The persistence layer needs a database-friendly representation without weakening those rules.

## Decision

Keep the domain model rich and behaviour-oriented, while the EF mapping layer translates it to relational storage. Value objects such as `Money`, `Sku`, `BranchCode` and `Weight` are mapped with converters, and audit metadata is handled through EF configuration rather than by allowing arbitrary over-posting.

This means the domain remains the source of business meaning while EF remains a storage concern.

## Consequences

- Stronger domain rules and cleaner business invariants
- Safer persistence mapping with explicit conversion boundaries
- Clear separation between business meaning and database representation
