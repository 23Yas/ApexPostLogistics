# Security – ApexPost Logistics API

## Authentication design

- **JWT Bearer tokens** issued for couriers and admins. Validation checks issuer, audience, lifetime and signing key — all read from configuration (`Jwt:Issuer`, `Jwt:Audience`, `Jwt:Key`). Development tokens are issued via the Development-only `POST /dev/token` endpoint and carry a `courierId` claim plus a `Courier` or `Admin` role.
- **Roles:** scan upload and status PATCH require `Courier`; DELETE requires `Admin`; GET requires any authenticated user. Missing/invalid/expired tokens return 401; wrong role returns 403 — both as `ProblemDetails`. A courier may upload scans only under their own `courierId` claim.
- **Partner API keys:** the read-only route `GET /api/v2/partner/shipments/{trackingNumber}` validates an `X-Api-Key` header against keys stored in User Secrets / configuration (`PartnerApiKeys` section). Keys are never committed. Missing or invalid keys return 401.

## OAuth2 flows (concepts)

- **Authorization Code + PKCE** (courier app): the app opens a system browser to the identity provider's authorize endpoint with a `code_challenge`. The user logs in and consents; the IdP redirects back with an authorization code. The app exchanges the code (plus `code_verifier`) for tokens. PKCE prevents authorization-code interception on mobile devices where the redirect URI is not secret.
- **Client Credentials** (partner systems): a partner server authenticates directly with its client id/secret to the token endpoint and receives an access token — no user interaction.
- **Roles:** the *identity provider* authenticates the user and issues tokens; the *access token* (JWT) proves identity/roles to the API on every call; the *refresh token* obtains new access tokens without re-login and is stored securely, rotated on use.
- **Why API keys are weaker than OAuth2:** API keys are static shared secrets — no expiry, no audience scoping, no user identity, and rotation requires redeploying every consumer. OAuth2 tokens are short-lived, scoped, carry identity, and can be revoked centrally.
- **Moving to a real IdP:** replace the `/dev/token` dev tokens with an OIDC provider (e.g. Entra ID, Auth0, Keycloak); point `Jwt:Issuer`/`Jwt:Audience`/signing keys at the IdP's metadata/JWKS; map IdP roles/groups to Courier/Admin; use the partner client-credentials flow for machine access.

## Transport layer security

- `UseHttpsRedirection()` everywhere; `UseHsts()` outside Development.
- Bearer tokens and API keys must never travel over plain HTTP: they are credentials. Over HTTP they can be sniffed and replayed. HTTPS (TLS) encrypts the channel so tokens cannot be read or modified in transit.
- The Bearer security scheme is declared in the v2 OpenAPI document and tested via the Swagger UI Authorize button.

## Certificate handling in development

Local development uses the ASP.NET Core dev certificate. Certificate validation is **never** disabled in Release builds. Any localhost bypass for the dev certificate is allowed only inside `#if DEBUG` blocks in the mobile app, and is documented here rather than weakening production TLS.
