# ADR 001: Hybrid layered architecture

- Status: Accepted
- Date: 2026-10-24

## Context

The Week 2 assignment requires a layered architecture that separates domain logic from infrastructure, while still keeping the MVC web application simple and testable.

## Decision

Use a hybrid model with:

- ApexPost.Core for domain and application logic
- ApexPost.Infrastructure for JSON repositories and simulated integrations
- ApexPost.Web for MVC, middleware and security concerns

## Consequences

- Clear separation of business logic from web and persistence concerns
- Easier migration to a database in future sprints
- Slightly more project structure, but much better maintainability
