# ADR 005: Action-level audit and correlation tracing

- Status: Accepted
- Date: 2026-10-24

## Context

The business requires traceability for branch actions such as sales, voids, refunds and identity verification, without storing sensitive details.

## Decision

Record audit events in an action filter with controller/action metadata, route values, branch, role and correlation ID. Persist the result to a JSON audit store, with sensitive fields redacted.

## Consequences

- Every key user action remains traceable
- No passwords, document numbers or API keys are stored in clear text
- Future reporting can query the audit log without exposing sensitive data
