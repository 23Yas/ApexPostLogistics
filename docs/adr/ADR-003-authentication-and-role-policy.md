# ADR 003: Cookie authentication and role policies

- Status: Accepted
- Date: 2026-10-24

## Context

The project requires staff authentication with branch-aware and role-aware authorization without ASP.NET Core Identity.

## Decision

Use custom claim-based cookie authentication and explicit authorization policies such as:

- BranchManagerOnly
- SameBranchOnly

## Consequences

- No dependency on ASP.NET Core Identity
- Easy role and branch checks in controllers and middleware
- Clear claim model: StaffId, Role and BranchCode
