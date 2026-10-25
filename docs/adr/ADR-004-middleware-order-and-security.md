# ADR 004: Security middleware ordering

- Status: Accepted
- Date: 2026-10-24

## Context

The web platform must be secure, observable and resilient under load while handling integration traffic and admin endpoints separately.

## Decision

Place exception handling, correlation IDs, request logging and maintenance checks before routing, then enforce rate limiting and security protections before auth and controller code.

## Consequences

- Safer failure handling for all endpoints
- Better operational traceability with correlation IDs
- Better protection against abuse and integration risk
