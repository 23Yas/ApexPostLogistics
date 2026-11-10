# Resilience – ApexPost Courier Mobile App

## Chosen Polly configuration

| Policy | Value | Reason |
|---|---|---|
| Retry | 3 attempts, exponential backoff with jitter (e.g. 2s, 4s, 8s ± jitter) | Covers transient mobile-network failures without hammering the API; jitter avoids thundering-herd retries when connectivity returns for many devices at once. |
| Retry triggers | Network errors, HTTP 408, HTTP 5xx only | 4xx responses are client errors (bad request, unauthorized, not found) — retrying them can never succeed and may duplicate side effects. |
| Circuit breaker | Opens after 5 consecutive failures; 30 s break; closes on the first successful call after the break | Stops the app wasting battery/data on a dead API and surfaces "Server unavailable – working offline" quickly. |
| Timeout | 10 s per attempt; 30 s overall request timeout | Per-attempt timeout keeps a single hung request from consuming the whole budget; the overall timeout bounds the entire operation including retries. |

## Why scan upload is safe to retry

The scan upload is **idempotent**: each scan carries the Part A scan ID (`scan::{Guid}`) and the server de-duplicates on that ID (Part B `UploadScans` returns `duplicatesSkipped`). Retrying a request that actually succeeded server-side but whose response was lost therefore cannot create duplicate scans — the server recognises the IDs and skips them. This is why the upload endpoint is allowed on the retry list even though it is a POST.

## Logging

Every retry attempt is logged (attempt number, delay, reason) and every circuit-breaker state change (open/half-open/closed) is logged, so sync problems can be diagnosed in the field without exposing tokens, API keys or full request bodies.
