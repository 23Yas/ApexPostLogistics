# \# ADR 017: Disposable test database

# 

# \- Status: Accepted

# \- Date: 2026-11-02

# 

# \## Context

# 

# The brief forbids tests from using a shared or development database. Tests that change data would corrupt demonstration data and make results depend on run order.

# 

# \## Decision

# 

# `CustomWebApplicationFactory` names a database `ApexPostTest\_<guid>` for each test class, runs the migrations and seed in it, and drops it on dispose. Connection strings come from in-memory configuration that overrides the development settings. `ApexPostDb` is never referenced by the test project.

# 

# \## Consequences

# 

# \- Each test class starts from a known state. Tests that change stock set the values they depend on.

# \- LocalDB must be running to run the tests. The database is dropped even if a test fails, because disposal runs in a `finally`-style teardown.

# \- Connection pools are cleared before the drop, so SQL Server does not refuse it while connections are open.

