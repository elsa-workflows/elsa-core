# Research: External Authentication

## 1. Module and Package Boundaries

**Decision**: Add `Elsa.ExternalAuthentication` as the focused Core/server module containing protocol-neutral contracts, configuration sources, the effective registry, broker services, FastEndpoints endpoints, security notifications, and in-memory development stores. Add `Elsa.ExternalAuthentication.OpenIdConnect` as the startup-installed v1 adapter and `Elsa.ExternalAuthentication.Secrets` as the optional Elsa Secrets binding bridge. Add `Elsa.ExternalAuthentication.UnitTests` and `Elsa.ExternalAuthentication.IntegrationTests`.

In the Studio repository, add:

- `Elsa.Studio.ExternalAuthentication` for Refit clients, DTOs, Login Method chooser, safe return-path handling, connection and link administration, descriptor-driven forms, navigation, and shared token abstractions.
- `Elsa.Studio.ExternalAuthentication.BlazorServer` for confidential-client exchange, server-held tokens, cookie session, callback, refresh, and logout.
- `Elsa.Studio.ExternalAuthentication.BlazorWasm` for public-client PKCE exchange, rotating refresh, and browser token access.

Persisted external-authentication entities live in the existing `IdentityElsaDbContext` and provider-specific Identity migrations. The persistence integration stays in `Elsa.Persistence.EFCore/Modules/Identity` so JIT user creation and External Identity Link creation share one database transaction.

**Rationale**: This matches the constitution's focused-module rule and existing Identity, Secrets, and Studio authentication package conventions. A separate adapter package proves the startup-installed extension boundary. Colocating Identity and external-link persistence is the smallest reliable way to make JIT atomic.

**Alternatives considered**:

- Put everything in `Elsa.Identity`: rejected because provider brokering, connection management, and extension contracts form a distinct feature boundary.
- A standalone external-authentication EF context: rejected for v1 because it cannot atomically create the existing Identity `User` and external link without cross-context transaction plumbing.
- One Studio package: rejected because Server and WebAssembly have incompatible session and token-storage responsibilities.
- Separate shared Studio authentication and management packages: rejected as premature separation because one shared Razor class library can own both broker UI and management while host adapters remain isolated.

## 2. Effective Connection Registry

**Decision**: Compose `IIdentityProviderConnectionSource` implementations into `IIdentityProviderConnectionRegistry`. V1 sources are deployment configuration and an optional store source. The registry resolves host-wide plus target-tenant connections, rejects database create/update collisions with existing configuration, and reports later configuration collisions as shadowed database records.

Registry reads remain read-through for v1: discovery and initiation read the current source versions instead of relying on an unbounded local cache. A shared monotonic registry version supports efficient conditional refresh later without becoming a security boundary.

**Rationale**: The existing Identity feature chooses either configuration or store providers. A composable registry is required to satisfy the merged ownership model and makes current-state checks deterministic across nodes.

**Alternatives considered**:

- Copy configuration entries into the database: rejected because it obscures ownership and risks secret migration.
- Cache indefinitely with pub/sub invalidation: rejected because missed invalidation can expose a disabled or stale connection.

## 3. Persistence and Atomicity

**Decision**: Define store contracts around required atomic operations rather than generic unconditional saves:

- Compare-and-swap connection mutation by revision.
- Unique `(TenantId, Key)` connection creation.
- Unique `(TargetTenantId, ConnectionId, Issuer, Subject)` External Identity Link creation.
- Atomic JIT `CreateLinkOrGetExistingAsync`.
- Atomic single-use `TryTakeAsync` for state, completion codes, preview results, and refresh-token rotation.

The EF Core provider uses database constraints and transactions. In-memory providers implement the same semantics for single-node development and unit tests. Multi-node deployment requires a shared provider.

**Rationale**: Current `IUserStore.SaveAsync` cannot make concurrent JIT creation or one-time code consumption safe. Atomic intent must be part of the public persistence contract, not inferred from a particular database.

**Alternatives considered**:

- Catch uniqueness exceptions around independent user/link saves: rejected because it cannot reliably converge or clean up an orphaned JIT user.
- Use `IDistributedCache` directly: rejected because its standard contract does not provide atomic take or refresh-token compare-and-swap.

## 4. Broker State and Credential Model

**Decision**: Use cryptographically random opaque handles. Persist only keyed hashes plus protected state in `IExternalAuthenticationStateStore`; consume handles atomically.

Default lifetimes:

| Artifact | Default | Behavior |
| --- | --- | --- |
| Provider correlation/state | 10 minutes | Single-use at callback |
| Completion code | 60 seconds | Single-use at token exchange |
| Preview state/result | 10 minutes | Result readable once by initiating administrator |
| External access token | Existing `IdentityTokenOptions.AccessTokenLifetime` | Default remains one hour for compatibility |
| External refresh inactivity | Existing `IdentityTokenOptions.RefreshTokenLifetime` | Default remains two hours |
| Maximum external session age | 8 hours | Never extended by refresh rotation |

External access tokens remain Elsa JWTs and include an External Authentication Session identifier. External refresh tokens are opaque, session-bound, rotated on every use, and revoke the token family on reuse. Existing local JWT refresh tokens and `/identity/refresh-token` remain unchanged.

**Rationale**: Opaque, atomically consumed handles prevent redirect leakage from becoming direct credential leakage and provide revocation. Separate external refresh behavior preserves existing local API clients.

**Alternatives considered**:

- Reuse the existing stateless JWT refresh endpoint: rejected because it cannot enforce connection disablement, session age, claim snapshots, or revocation.
- Put full broker state in a protected browser value: rejected because one-time consumption and server-side revocation would remain weak.

## 5. Token Issuance Refactoring

**Decision**: Extract the JWT construction portion of `DefaultAccessTokenIssuer` into a shared token issuance service accepting a `TokenIssuanceContext` with user, claims, roles, permissions, token use, and optional session identity. Keep `IAccessTokenIssuer.IssueTokensAsync(User)` unchanged and delegate its existing local behavior to the shared service. Add an external issuer that evaluates Permission Grant Sources and issues the external access token plus opaque refresh token.

**Rationale**: This avoids ambient session state and keeps the existing public Identity contract stable while removing duplicate JWT construction.

**Alternatives considered**:

- Add a breaking parameter to `IAccessTokenIssuer`: rejected because Elsa is an embeddable library with public API compatibility requirements.
- Duplicate token creation in the new module: rejected because signing and claim conventions would drift.

## 6. OpenID Connect Adapter

**Decision**: Implement the adapter against Microsoft IdentityModel OpenID Connect protocol primitives rather than dynamically adding ASP.NET Core authentication schemes per connection. The adapter owns discovery/manual metadata, authorization request construction, code exchange, ID-token validation, optional user-info retrieval, claim projection, test, preview, and upstream logout.

The adapter follows OpenID Connect Core and OAuth 2.0 Security BCP:

- Authorization-code flow only.
- Exact provider callback URI.
- State/correlation and nonce validation.
- Signature, issuer, audience/authorized-party, lifetime, and subject validation.
- PKCE to the provider by default; an unsafe override can disable it only when deployment policy permits.
- No implicit flow or password grant.
- Mix-up protection by binding and validating issuer.
- Refresh/provider tokens discarded after normalized identity creation unless needed transiently for configured user-info retrieval.

**Rationale**: Runtime connection data does not map cleanly to startup-only authentication schemes. A protocol adapter contract also needs to serve future OAuth providers that are not OpenID Connect handlers.

**Alternatives considered**:

- Register one dynamic ASP.NET authentication scheme per connection: rejected because runtime add/remove, per-tenant resolution, revision binding, and draft preview become complex and handler-specific.
- Build JWT validation primitives from scratch: rejected in favor of maintained IdentityModel validation.

**Standards evidence**:

- [OpenID Connect Core 1.0](https://openid.net/specs/openid-connect-core-1_0.html) defines code-flow, nonce, state, redirect, and ID-token validation requirements.
- [RFC 9700](https://www.rfc-editor.org/rfc/rfc9700) defines current OAuth security best practices including PKCE, exact redirect handling, mix-up defenses, and refresh-token protections.
- [ASP.NET Core OpenID Connect guidance](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-oidc-web-authentication?view=aspnetcore-10.0) recommends code flow with PKCE and a confidential BFF for server-hosted web applications.

## 7. Authentication Clients and Broker Endpoints

**Decision**: Authentication Clients are deployment configuration, separate from Elsa API Applications. Confidential clients use a Secret Binding for client authentication; public clients have no secret. Both require S256 PKCE and exact callback/logout URI registration.

Use OAuth-shaped broker endpoints under `/external-authentication` while keeping them Elsa-specific rather than presenting a general-purpose OAuth authorization server:

- Login Method discovery.
- External and local broker initiation.
- Fixed provider callback per immutable Connection ID.
- Authorization-code and external-refresh exchange.
- Session logout and optional upstream logout callback.

**Rationale**: Familiar OAuth-shaped messages are easier to secure and test, while explicitly limiting the server to registered Elsa clients avoids claiming general authorization-server conformance.

**Alternatives considered**:

- Reuse `Application`: rejected because API Applications carry roles/permissions and represent machine/API credentials.
- Return Elsa tokens directly from provider callbacks: rejected because tokens would pass through browser redirects.

## 8. Studio Hosting Profiles

**Decision**:

- **Server**: implement a BFF-style confidential client. The Studio host owns callback/exchange, stores Elsa access/refresh tokens in its protected server session, and issues only an HTTP-only, Secure, SameSite=Lax cookie with antiforgery protection. Cookie lifetime cannot exceed external session lifetime.
- **WebAssembly**: implement a public browser client. The token endpoint permits only exact registered origins, no wildcard and no credentialed CORS, requires S256 PKCE, and returns a rotating refresh token. The pre-exchange transaction uses `sessionStorage` so it survives the provider redirect and is deleted after callback. Post-exchange credentials stay in memory by default; explicitly warned session or durable storage policies are configurable alternatives.

The deployment selects one authentication provider: `ElsaIdentity`, `ElsaLogin` (legacy), `OpenIdConnect` (direct), or `ExternalAuthentication` (brokered). The management module can still be installed independently.

**Rationale**: The two Studio hosts have different trust boundaries. Current Studio already selects one provider branch, so a new explicit mode preserves deterministic route and handler ownership.

**Alternatives considered**:

- Use a public browser flow for Studio Server: rejected because a BFF avoids exposing tokens to browser code.
- Require a BFF for standalone WebAssembly: rejected because the supported static-host deployment has no Studio-side server component.

**Standards evidence**: The current [OAuth Browser-Based Applications draft](https://datatracker.ietf.org/doc/draft-ietf-oauth-browser-based-apps/) ranks BFF as the strongest pattern and requires browser refresh tokens to rotate or be sender-constrained with a bounded lifetime.

## 9. Local Credentials and Credential-less Users

**Decision**: Make `User.HashedPassword` and `User.HashedPasswordSalt` nullable as the compatible first migration. `DefaultUserCredentialsValidator` returns the same invalid result when either is absent. Existing `/identity/login` and `/identity/refresh-token` contracts stay unchanged.

Add a new broker-local initiation endpoint for registered Authentication Clients. It validates credentials through the existing service and returns the same completion-code handoff as external authentication.

JIT uses an internal collision-resistant user name derived from the tenant and link identity plus random suffix; provider email and display name remain optional profile inputs, never identity keys. The current global user-name uniqueness rule remains.

**Rationale**: This is the smallest compatible model change and avoids placeholder passwords or a breaking user-table redesign.

**Alternatives considered**:

- Extract Local Credentials into a new table immediately: deferred because nullable password fields satisfy v1 without migrating every local user.
- Change user names to tenant-scoped uniqueness in the same feature: deferred as an independent Identity migration.

## 10. Identity Links and Unlinked Policies

**Decision**: Persist External Identity Links separately with a durable unique identity tuple. Built-in policies are `Reject` and `CreateUser`. Custom policies may return a target-user decision only through a typed result containing the user ID and authorization basis; the broker still enforces tenant match and link uniqueness.

Administrator prelinking uses the same atomic link service as JIT. End-user self-linking is not implemented.

**Rationale**: A distinct link keeps provider identity separate from Elsa authorization and lets one Elsa User own multiple external identities.

## 11. Permission Composition

**Decision**: Evaluate ordered `IPermissionGrantSource` implementations into a provenance-bearing result, then union distinct permission strings. V1 sources are current Elsa role permissions, claim mappings, and group mappings. Pass-through requires an explicit allowlist or pattern boundary.

Grant configuration authorization reuses the principle in `RoleAuthorizationService`: the actor must possess every concrete permission they may delegate, unless they have `external-authentication:permissions:delegate-unrestricted`. Deployment allow/deny boundaries apply after actor authorization.

`IPermissionDescriptorProvider` is optional metadata for Studio pickers and warnings. Unknown strings remain storable.

**Rationale**: Elsa's permission vocabulary is open and modular; descriptors cannot become an authority.

## 12. Secret Bindings

**Decision**: Define `ISecretBindingResolver` in the protocol-neutral module, returning a value plus a nonreversible generation fingerprint. Provide:

- An Elsa Secrets resolver in `Elsa.ExternalAuthentication.Secrets`, using immutable secret name and latest active version.
- A configuration resolver using a configuration key; the broker derives a keyed fingerprint from the resolved value when the provider cannot supply a version.

The fingerprint participates in effective material revision but is never returned, logged, or persisted as public connection data.

**Rationale**: Every resolver must expose equivalent rotation semantics so callback revision checks do not depend on provider-specific version features.

## 13. Provider HTTP Safety

**Decision**: Use a dedicated named `HttpClient` pipeline and destination validator for discovery, token, user-info, test, preview, and logout requests.

Secure defaults:

- HTTPS required; HTTP only through an explicit development/unsafe policy.
- Private, loopback, link-local, multicast, reserved, and unspecified addresses denied after each DNS resolution.
- DNS is resolved again for every redirect target; redirects are limited to three and may not downgrade HTTPS.
- Connect/request timeout: 10 seconds.
- Discovery/JWKS maximum: 1 MiB each.
- Token/user-info maximum: 256 KiB each.
- Optional deployment allowlist and egress proxy take precedence over connection settings.
- Response bodies never flow to public errors, previews, health details, logs, or notifications.

**Rationale**: Runtime-configured authorities make discovery and test endpoints SSRF-capable unless outbound policy is a first-class boundary.

## 14. Rate Limiting and Public Errors

**Decision**: Integrate with ASP.NET Core named rate-limiter policies and provide secure reference defaults:

| Endpoint family | Default partition | Default limit |
| --- | --- | --- |
| Discovery | client ID + remote network | 60 requests/minute |
| External initiation | client ID + remote network | 20 requests/minute |
| Local initiation | client ID + remote network + keyed user-name hash | 10 requests/minute |
| Provider callback | Connection ID + remote network | 60 requests/minute |
| Code/refresh exchange | client ID + remote network | 30 requests/minute |

429 responses include `Retry-After` and a safe `rate_limited` category. Other public categories are `invalid_request`, `method_unavailable`, `authentication_failed`, `identity_unlinked`, `flow_expired`, `flow_changed`, `access_denied`, `temporarily_unavailable`, and `server_error`, each with a correlation identifier.

**Rationale**: Named policies let hosts replace limits while preserving explicit, testable defaults and non-enumerating errors.

## 15. Connection Testing and Preview

**Decision**: Store only the latest redacted `ConnectionObservation` in a shared store keyed by Connection ID and tested material revision. It becomes stale when the effective material revision differs; no history or polling is added.

Preview uses its own client/callback purpose and atomic state/result records. The result allowlist contains issuer, subject hash or masked subject, selected projected claims with descriptor redaction, target tenant, proposed policy decision, proposed user/link action, permission grants with provenance, and warnings. It cannot be exchanged for a normal session.

**Rationale**: Operators need a useful current observation and interactive diagnosis without creating an audit/monitoring subsystem or authentication side effects.

## 16. Security Notifications and Observability

**Decision**: Publish typed `INotification` records through `INotificationSender` after committed security-relevant outcomes. Also emit structured logs, metrics, and tracing with correlation ID, tenant, Connection ID, adapter type, outcome category, and duration—but never secrets, tokens, raw subjects, or unrestricted claims.

Use an outbox only when the host/audit subscriber requires durable notification delivery; it is not part of the base module.

**Rationale**: This aligns with current Elsa Mediator and leaves audit retention to an optional module.

## 17. Performance and Scale Targets

**Decision**:

- Support 10,000 persisted connections across all tenants and 50 effective Login Methods per tenant.
- Return a 100-row management page within 500 ms p95 under normal indexed database load.
- Return Login Method discovery within 250 ms p95 at 100 concurrent requests.
- Add no more than 250 ms p95 Elsa processing overhead to initiation, callback completion, or token exchange, excluding provider/network latency.
- Stream or page all management collections; never load all tenants' connections or links for a normal tenant request.

**Rationale**: These limits are high enough for enterprise multi-tenancy while preventing unbounded collection and UI behavior.

## 18. Delivery and Migration

**Decision**: Deliver in three implementation milestones but keep one feature specification:

1. Configuration-first broker foundation with both Studio hosts.
2. Persisted administration, descriptors, testing, and preview.
3. Tenant/link/permission/recovery/cluster hardening and compatibility documentation.

Direct OpenID Connect remains untouched until deployment selects `ExternalAuthentication`. Migration documentation maps settings but never copies secrets.

**Rationale**: Each milestone is demonstrable and testable while the complete feature remains the v1 definition of done.
