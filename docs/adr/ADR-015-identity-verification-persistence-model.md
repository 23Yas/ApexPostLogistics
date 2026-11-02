# ADR 015: Separate persistence model for IdentityVerification

- Status: Accepted
- Date: 2026-11-02

## Context

Identity documents must be masked everywhere, including the database, logs and audit entries. The domain object carries a masked string, but nothing stops that string being longer than intended.

## Decision

`IdentityVerificationRecord` is a separate persistence class. `IdentityVerificationMapper` stores only the last four characters in `MaskedLast4` (varchar(4)). The schema therefore cannot hold a full document number, whatever the domain does.

The domain class `IdentityVerification` is not mapped by EF. It is mapped by hand in `IdentityVerificationMapper`.

## Consequences

- Two classes describe the same concept. The mapper is small and tested by round trip.
- Masking is enforced by the schema, not only by validation code.
- `MaskedIdentifierPolicy` (Domain) rejects all-digit values, which look like full document numbers.
