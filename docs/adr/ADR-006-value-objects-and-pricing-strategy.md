# ADR 006: Value objects and pricing strategy

- Status: Accepted
- Date: 2026-10-24

## Context

The domain must distinguish money, branch codes, SKUs and shipping weights clearly to avoid invalid calculations and ambiguous identifiers.

## Decision

Use strongly typed value objects such as Money, BranchCode, Sku and Weight, and centralize pricing logic in application services.

## Consequences

- Better enforcement of domain correctness
- Easier business rule checks for tax, service level and stock logic
- Clearer code and simpler future extension of pricing rules
