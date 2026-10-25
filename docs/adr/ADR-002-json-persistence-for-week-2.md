# ADR 002: JSON persistence for Week 2

- Status: Accepted
- Date: 2026-10-24

## Context

The assignment explicitly says there is no database week, so persistence must be simulated using JSON files behind repository interfaces.

## Decision

Store domain data as JSON files and access them through repository abstractions.

## Consequences

- Business logic stays decoupled from storage technology
- Next week's database layer can replace repositories with minimal application change
- The solution remains lightweight for teaching and local demos
