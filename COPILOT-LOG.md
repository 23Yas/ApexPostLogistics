# Copilot / AI-Assisted Test Log – Week 5

> Note: the drafting assistant here was Devin (an AI agent), not GitHub Copilot —
> the prompts, outputs and review decisions below are recorded the same way the
> brief asks for. Each prompt is preserved verbatim.

## Prompt 1 – unit tests for a service

**Prompt:**
> Write xUnit + Moq unit tests for `ShipmentService` in `ApexPost.Application`.
> Cover: page validation, duplicate tracking on create, invalid status transition,
> delete rules, and scan upload outcomes (accepted / duplicate / rejected).
> Use AAA, `Method_Scenario_Expected` naming, and `Verify(..., Times.Never)` where
> a rejected operation must not reach the repository.

**What was produced:** `ApexPost.UnitTests/ShipmentServiceTests.cs` — 19 tests.

**Review decisions:**

| Decision | Detail |
|---|---|
| Kept | All repository-call verifications (`Times.Once`/`Times.Never`) — they prove rejected paths never persist. |
| Kept | `GetPagedAsync` boundary theory (pageSize 0 and 51). |
| Changed | Initial draft asserted on exception *type only*; added `Assert.Contains("page", ex.Message)` where the message carries the contract. |
| Changed | The "future scan" test first mutated `ScannedAtUtc` after construction — impossible (`init` setter); rewrote to construct the scan with a future timestamp in the initializer. |
| Rejected | A generated test that stubbed `GetScanByIdAsync` to return the same scan *and* expected `Accepted == 1` — that contradicts the idempotency rule; the correct expectation is `DuplicatesSkipped == 1`. |

## Prompt 2 – Gherkin feature + step definitions

**Prompt:**
> Write a Reqnroll feature `ScanSync.feature` in business language for offline
> scan upload: valid scans accepted, duplicate scan skipped, invalid scan
> rejected, terminal scans only from OutForDelivery. Include a Background and a
> Scenario Outline. Then write the step definitions.

**What was produced:** `ApexPost.Specs/Features/ScanSync.feature` + `ScanSyncSteps.cs`.

**Review decisions:**

| Decision | Detail |
|---|---|
| Kept | Background + Scenario Outline structure; business-language scenario names. |
| Changed | Draft used static fields for the HttpClient — replaced with instance fields (Reqnroll creates a binding instance per scenario) per the "no static state" constraint. |
| Changed | Draft PATCHed shipment status with the admin token — the API correctly returned 403 (status is Courier-only); switched transitions to the courier client. This was the draft *copying an incorrect assumption*, caught in review. |
| Changed | Draft tracking numbers `APX-7100002` had 7 digits; the API regex requires `APX-\d{8}` — padded examples. |
| Changed | Draft sent `notes: ""` on Delivered scans — API validation requires a signature; step now sends `signature:BDD-Recipient`. |

## Prompt 3 – HTTP client tests

**Prompt:**
> Write tests for `ApexApiClient` using a fake `HttpMessageHandler`: 200 JSON,
> 404, 500, timeout, and a ProblemDetails body. No real network.

**What was produced:** `ApexPost.UnitTests/ApexApiClientTests.cs` — 7 tests.

**Review decisions:**

| Decision | Detail |
|---|---|
| Kept | All seven cases; the fake handler records nothing it doesn't need — kept minimal. |
| Changed | Draft asserted `ex.StatusCode == 400` on the ProblemDetails case — `HttpRequestException.StatusCode` is nullable and the client wraps the status; assertion changed to message content (`detail`) which is the actual contract. |
| Added | `CreateShipmentAsync_401_RaisesUnauthorizedEvent` — the draft missed the 401-event path entirely. |

## Proof the generated tests can fail (mutation testing)

1. Raised `ShipmentService.MaxScansPerBatch` from `200` to `500`.
2. `dotnet test --filter UploadScansAsync_MoreThan200Scans` → **FAILED**
   (`Assert.Throws` — no exception thrown), proving the test guards the rule.
3. Restored `200` → test passes again.

A second mutation was performed during development: the `ApiJsonContext` naming
bug (`new ApiJsonContext()` bypassing `JsonSourceGenerationOptions`) was caught
*by* `ApexApiClientTests.GetShipmentAsync_200_ReturnsShipment` returning empty
strings — the tests found a real production bug, not just guarded behaviour.

AI-assisted test classes are marked with a comment (`ApexApiClientTests.cs`).
