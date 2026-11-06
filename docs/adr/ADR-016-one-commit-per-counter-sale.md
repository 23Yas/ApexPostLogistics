# \# ADR 016: One commit per counter sale

# 

# \- Status: Accepted

# \- Date: 2026-11-02

# 

# \## Context

# 

# A sale changes stock, creates the transaction and its lines, and records the payment. A failure between these writes must not leave stock reduced without a sale, or a sale without its payment.

# 

# \## Decision

# 

# `CheckoutService.CheckoutAsync` validates every line first, with lines for the same SKU added together, and throws `DomainException` before any write if stock is insufficient. Then it changes stock through the aggregate (`StockItem.Remove`), stages the transaction and its payment, and calls `SaveChangesAsync` once. Notifications are published only after that commit.

# 

# \## Alternatives considered

# 

# \- A manual transaction around several `SaveChanges` calls. Rejected: one `SaveChanges` is already atomic, and a second commit would add a failure point.

# \- Validating only in the controller. Rejected: the rule belongs to the use case, so it is tested without a web host.

# 

# \## Consequences

# 

# \- Unit tests prove the commit count: one on success, none on failure (`CheckoutServiceTests`).

# \- The database test `FailedSale\_RollsBack\_StockAndTransactionTogether` proves that a failure at commit leaves no partial rows.

# \- Concurrent sales of the last unit are handled by the stock rowversion (ADR-013).

