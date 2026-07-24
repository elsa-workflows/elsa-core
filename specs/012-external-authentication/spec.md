# Feature Specification: External Authentication

**Feature Branch**: `codex/012-external-authentication`

**Created**: 2026-07-24

**Status**: Draft

**Input**: Deliver Elsa 3 external authentication and Identity Provider Connections end to end across Elsa Core and Elsa Studio, based on [the approved PRD](prd.md), [domain language](../../CONTEXT.md), and [architecture decisions](../../docs/adr).

## Product Context

Elsa supports local credentials, Elsa-issued tokens, API keys, and a startup-configured direct OpenID Connect option in Studio. It does not provide a server-owned broker where deployers and authorized administrators can compose multiple external authentication choices, manage selected connections at runtime, link external identities to Elsa Users, and preserve Elsa authorization.

This feature adds **External Authentication** as an Elsa capability. An **Identity Provider Connection** describes Elsa's trust relationship with an external provider. Connections can be deployment-owned configuration or optionally administrator-owned persisted data. Elsa Server brokers sign-in and issues Elsa credentials; Studio discovers Login Methods and supplies management experiences without receiving provider secrets.

OpenID Connect is the first adapter. The connection and extension contracts must also support future provider-specific OAuth adapters without changing the core connection model or client completion flow.

## Clarifications

### Session 2026-07-24

- Elsa Server owns provider redirects, callbacks, identity resolution, permission resolution, and Elsa credential issuance.
- Configuration-owned and database-owned connections form one effective registry; configuration is authoritative on a same-scope key collision.
- A connection uses a protocol-neutral envelope with adapter-owned, versioned settings and non-secret Secret Bindings.
- Host-wide scope uses Elsa's tenant-agnostic identity; default-tenant and tenant-specific connections remain distinct.
- External identities link to Elsa Users by target tenant, immutable Connection ID, validated issuer namespace, and stable subject—never by email or user name by default.
- Unknown identities are governed by a deployer-controlled, extensible Unlinked Identity Policy; the safe default rejects them.
- Elsa permissions are composed from explicit Permission Grant Sources. Permission Descriptors help authoring but are not an authoritative permission catalog.
- Local Elsa credentials and enabled external connections are unified as Login Methods, but local login is not modeled as an Identity Provider Connection.
- Both Studio Server and Studio WebAssembly use the broker completion-code flow with host-appropriate session handling.
- Existing direct Studio OpenID Connect remains available as a mutually exclusive deployment mode during migration.
- Connection testing is on demand. Continuous health monitoring and history are outside v1.
- Security events are published for audit subscribers; this feature does not own an audit database.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Sign In Through an External Provider (Priority: P1)

An Elsa user selects an enabled external Login Method, authenticates with the provider, and returns to Studio with an Elsa session containing the permissions Elsa grants to the linked user.

**Why this priority**: Brokered sign-in is the core user value and the foundation for all management and extension work.

**Independent Test**: Configure one OpenID Connect connection through deployment configuration, discover it from Studio, complete sign-in, and verify the resulting Elsa principal is tenant-scoped and contains Elsa-issued permissions without exposing provider or Elsa tokens in redirects.

**Acceptance Scenarios**:

1. **Given** an enabled, valid connection and a linked identity, **When** a user completes provider authentication, **Then** Elsa resolves the link and returns a short-lived, single-use completion code to the registered client.
2. **Given** an unknown identity and the reject policy, **When** provider authentication succeeds, **Then** Elsa denies access with a safe error and correlation identifier.
3. **Given** an unknown identity and an allowed just-in-time policy, **When** provider authentication succeeds, **Then** Elsa atomically creates a credential-less Elsa User and link before issuing Elsa credentials.
4. **Given** a connection changes materially after initiation, **When** its callback arrives, **Then** Elsa rejects the flow rather than completing against the new settings.
5. **Given** two callbacks for the same previously unknown External Identity arrive concurrently, **When** JIT provisioning runs, **Then** both converge on one tenant-scoped link and Elsa User or one safely retries after observing the winning transaction.

---

### User Story 2 - Manage Persisted Connections (Priority: P1)

An authorized connection administrator creates, reads, updates, enables, disables, archives, restores, tests, and previews database-owned Identity Provider Connections from Elsa Studio. Deployment-owned connections appear in the same list but remain read-only.

**Why this priority**: Runtime administration without restart is the main capability beyond existing direct OpenID Connect configuration.

**Independent Test**: Create a disabled OpenID Connect draft in Studio, supply settings and Secret Bindings, run test and preview, enable it, and verify it becomes discoverable across the cluster without restart.

**Acceptance Scenarios**:

1. **Given** an installed adapter, **When** an administrator creates a disabled draft, **Then** Studio renders its settings from the adapter description and permits incomplete draft storage.
2. **Given** a valid draft with resolvable required secrets, **When** it is enabled, **Then** it becomes an effective Login Method without restart.
3. **Given** a configuration-owned connection, **When** any runtime caller attempts to mutate it, **Then** Elsa rejects the operation.
4. **Given** two administrators edit one persisted connection, **When** the second submits a stale revision, **Then** Elsa reports a concurrency conflict without overwriting the first update.
5. **Given** a configuration connection later claims the same scope and key as a persisted connection, **When** the registry resolves, **Then** configuration wins and Studio shows the persisted record as shadowed.
6. **Given** configuration already owns a scope and key, **When** an administrator creates or renames a persisted connection to that key, **Then** Elsa rejects the mutation rather than creating a shadowed record.

---

### User Story 3 - Preserve Elsa Authorization (Priority: P1)

A security administrator links external identities to Elsa Users and configures explicit external-claim or group mappings. Elsa combines those bounded external grants with current Elsa-owned user and role grants.

**Why this priority**: External authentication must not bypass Elsa-specific roles and permissions.

**Independent Test**: Link a provider identity to an Elsa User, configure a bounded group mapping, sign in, and verify the Elsa token contains the expected union while an unmapped or unauthorized provider permission grants nothing.

**Acceptance Scenarios**:

1. **Given** a linked user with Elsa role permissions and an external group mapping, **When** sign-in completes, **Then** the Elsa credential contains their allowed effective union.
2. **Given** an external permission claim with no mapping or pass-through boundary, **When** sign-in completes, **Then** it grants no Elsa permission.
3. **Given** an administrator does not possess a permission and lacks unrestricted delegation authority, **When** they attempt to configure a mapping that can grant it, **Then** Elsa rejects the change.
4. **Given** an external session is refreshed, **When** upstream claims changed without reauthentication, **Then** the external snapshot remains unchanged while current Elsa-owned role grants are reevaluated.

---

### User Story 4 - Use the Same Broker from Studio Server and WebAssembly (Priority: P1)

Studio Server and Studio WebAssembly use the same Login Method discovery and broker completion model while applying the session protections appropriate to confidential server and public browser clients.

**Why this priority**: Both supported Studio hosting models must remain first-class.

**Independent Test**: Complete the same configured external sign-in from both hosts and verify exact callback matching, mandatory client-to-Elsa PKCE, correct session establishment, refresh handling, and logout.

**Acceptance Scenarios**:

1. **Given** a Studio Server client, **When** code exchange succeeds, **Then** the host retains Elsa refresh credentials server-side and the browser receives only a secure, HTTP-only Studio session.
2. **Given** a Studio WebAssembly client, **When** code exchange succeeds, **Then** no client secret is accepted and exact-origin cross-origin policy plus PKCE are enforced.
3. **Given** an absolute or protocol-relative return target, **When** login, recovery, preview, or logout validates it, **Then** the target is rejected.
4. **Given** both Direct OpenID Connect and Brokered External Authentication are selected, **When** Studio starts, **Then** startup fails with an actionable configuration error.
5. **Given** an existing client calls the current local login and refresh endpoints, **When** the broker feature is installed, **Then** their token-returning contract remains unchanged unless the client explicitly adopts the new broker-local completion route.

---

### User Story 5 - Operate Connections Safely (Priority: P2)

An operator can distinguish administrative enablement, structural validity, and the most recent on-demand test result, diagnose failures using safe correlation identifiers, and recover without depending on the connection being repaired.

**Why this priority**: Provider failure must be diagnosable without leaking secrets or locking administrators out.

**Independent Test**: Test a valid but unreachable connection, verify it stays administratively enabled, inspect the redacted result, then sign in through the independent recovery method and repair it.

**Acceptance Scenarios**:

1. **Given** a provider test fails, **When** Studio refreshes the connection, **Then** enabled intent is unchanged and the result is labeled with tested revision and timestamp.
2. **Given** a connection is materially updated, **When** Studio displays an older test result, **Then** it is marked stale.
3. **Given** final-login-path protection is active, **When** an administrator tries to disable the final valid method without recovery or privileged override, **Then** Elsa rejects the operation.
4. **Given** an external provider is unavailable, **When** an authorized operator uses an independent local or break-glass method, **Then** connection management remains reachable.

---

### User Story 6 - Extend Providers and Policies (Priority: P2)

A module author deploys a trusted Protocol Adapter, Unlinked Identity Policy, or Permission Grant Source. The server publishes its settings description so a standard Studio form works without provider-specific database columns or Studio code; an optional custom editor can improve the experience.

**Why this priority**: The initial OpenID Connect implementation must not prevent later GitHub or other provider-specific adapters.

**Independent Test**: Register a conformance adapter with unique settings and a secret field, configure it using the generic editor, authenticate a normalized identity, and complete the unchanged broker flow.

**Acceptance Scenarios**:

1. **Given** an installed adapter with described settings, **When** an administrator creates a connection, **Then** Studio renders and validates its common and adapter-specific fields.
2. **Given** the adapter has no custom Studio editor, **When** it is configured, **Then** the generic editor remains fully functional.
3. **Given** an adapter settings schema evolves, **When** an older persisted version loads, **Then** the adapter provides a compatible migration or a clear invalid state without corrupting the record.
4. **Given** a new adapter is installed, **When** it authenticates successfully, **Then** it produces the same protocol-neutral External Identity consumed by the broker.

---

### User Story 7 - Administer External Identity Links (Priority: P2)

An authorized security administrator searches tenant-scoped Elsa Users, prelinks a known external subject, inspects safe link metadata, or unlinks it with confirmation.

**Why this priority**: Preprovisioned-only environments need deliberate admission without full user and role management in this feature.

**Independent Test**: Find a user through the bounded picker, prelink a connection/issuer/subject tuple, complete external sign-in, unlink it, and verify subsequent sign-in follows the configured unlinked policy.

**Acceptance Scenarios**:

1. **Given** a tenant context and read permission, **When** the administrator searches users, **Then** only paginated minimal selection data for that tenant is returned.
2. **Given** a valid external identity tuple, **When** the administrator prelinks it to a user, **Then** the next matching sign-in resolves that user.
3. **Given** a link is removed, **When** the identity signs in again, **Then** Elsa applies the effective Unlinked Identity Policy.
4. **Given** a caller lacks identity-link permission, **When** they access the page or API, **Then** both route and operation authorization reject access regardless of menu visibility.
5. **Given** a selected Elsa User belongs to another tenant, **When** an administrator attempts to prelink it, **Then** Elsa rejects the link without revealing cross-tenant user details.
6. **Given** the external identity tuple is already linked, **When** another prelink or JIT operation targets a different user, **Then** the durable uniqueness rule rejects it.

---

### User Story 8 - Migrate Existing Direct OpenID Connect Deployments (Priority: P3)

A deployment owner can keep the current direct Studio OpenID Connect mode or deliberately migrate its settings to one configuration-owned broker connection without silent secret movement.

**Why this priority**: Adoption must not break existing Elsa 3 deployments.

**Independent Test**: Start Studio in Direct OpenID Connect mode after installing the feature, verify behavior is unchanged, then follow the migration guide and explicitly switch to brokered mode.

**Acceptance Scenarios**:

1. **Given** an existing direct deployment that has not selected brokered mode, **When** the new modules are installed, **Then** existing authentication remains unchanged.
2. **Given** a deployment follows the migration guide, **When** it creates the equivalent configuration-owned connection and explicitly changes mode, **Then** the brokered chooser replaces direct authentication.
3. **Given** migration is rolled back, **When** Direct OpenID Connect is selected again, **Then** its original settings remain usable.

### Edge Cases

- Anonymous discovery occurs without a trusted tenant context.
- A tenant-scoped key collides with an inherited host-wide key.
- Configuration shadows a previously enabled persisted connection.
- A callback or refresh reaches a different Elsa node than initiation.
- A connection is disabled, archived, materially updated, or has its secret rotated during sign-in.
- A disabled draft is incomplete or references a missing secret.
- A provider is structurally valid but unreachable.
- An OIDC discovery endpoint resolves to a private, loopback, link-local, reserved, redirected, or oversized response.
- An administrator loses their Studio session during Preview Sign-in.
- A credential-less Elsa User attempts local login.
- JIT provisioning races for the same external identity or generated user name.
- An external subject signs in to two target tenants.
- A provider changes mutable email, display name, or group claims.
- A permission mapping references an unknown but syntactically valid Elsa permission string.
- A connection is archived and later restored, or a new connection reuses its former display key.
- Automatic redirect fails and must return to the chooser without looping.
- The provider does not support upstream logout or its logout endpoint fails.
- Access tokens issued before disablement remain valid until their short expiry unless explicitly revoked.
- Management UI is visible to a user who may read but not test, preview, edit, or delegate permissions.
- A remote or malicious icon/display name attempts to spoof the login chooser.

## Requirements *(mandatory)*

### Functional Requirements

#### Connection Registry and Ownership

- **FR-001**: Elsa MUST expose External Authentication independently of any Studio host.
- **FR-002**: Elsa MUST present one effective registry composed from configuration-owned and optional database-owned Identity Provider Connections.
- **FR-003**: Configuration-owned connections MUST be inspectable but immutable through runtime management surfaces.
- **FR-004**: Database-owned connections MUST support create, read, update, enable, disable, archive, restore, test, and Preview Sign-in operations.
- **FR-005**: Creating or changing a database-owned connection for an installed adapter MUST take effect without server restart.
- **FR-006**: Configuration MUST win a collision on the same Connection Scope and Connection Key; the shadowed database record MUST remain visible for diagnosis.
- **FR-007**: Creating or renaming a database-owned connection to a `(Connection Scope, Connection Key)` already owned by configuration MUST be rejected. Shadowing applies only when configuration claims the key after the database record already exists.
- **FR-008**: Every connection MUST have an immutable Connection ID, a stable scope-local key, source ownership, scope, adapter type, presentation, lifecycle state, and revision.
- **FR-009**: Host-wide connections MUST use Elsa's tenant-agnostic scope, while default-tenant and tenant-specific scopes remain distinct.
- **FR-010**: A tenant-scoped key MUST NOT collide with an inherited host-wide key in that tenant's effective registry.
- **FR-011**: Archive MUST preserve the connection identity and links; restore MUST retain that identity, while a genuinely new connection MUST receive a new identity.
- **FR-012**: Database mutations MUST enforce optimistic concurrency.
- **FR-013**: Enabled intent, structural validity, and observed test result MUST remain separate states.
- **FR-014**: Incomplete disabled drafts MAY be saved; enabling MUST require valid settings and resolvable required secrets.
- **FR-015**: Provider test failure MUST NOT automatically disable or hide a valid enabled connection.
- **FR-016**: Anonymous discovery with a tenant MUST include applicable tenant and host-wide methods; discovery without a tenant MUST include host-wide methods only and reveal no tenant existence.

#### Extensibility, Settings, and Secrets

- **FR-017**: Protocol Adapters, Unlinked Identity Policies, and Permission Grant Sources MUST be trusted deployed extensions registered at startup. Runtime configuration MAY select only installed, deployment-allowed types; deploying new extension code MAY require restart.
- **FR-018**: Each extension type MUST expose a stable identifier and descriptor containing its settings schema version, fields, validation rules, UI hints, secret-binding metadata, conditional visibility, capabilities, presentation, and optional custom-editor contract key/version.
- **FR-019**: Studio MAY use a custom editor registered for an extension type, but absence of one MUST NOT prevent complete configuration.
- **FR-020**: Connections MUST use a protocol-neutral envelope with opaque, versioned adapter settings whose compatibility and migration are owned by the adapter.
- **FR-021**: Adapter-specific fields MUST NOT require provider-specific connection columns or tables.
- **FR-022**: Successful adapters MUST return a normalized External Identity containing a validated issuer namespace, stable subject, and bounded claims.
- **FR-023**: V1 MUST include a conforming OpenID Connect adapter.
- **FR-024**: The OpenID Connect adapter MUST use authorization-code flow and validate state, correlation, nonce, ID-token signature, issuer, audience or authorized party, expiry, callback errors, and provider-facing PKCE when enabled by effective Provider Trust Settings.
- **FR-025**: Future provider-specific OAuth or SAML adapters MUST be addable without changing the connection envelope or client completion contract.
- **FR-026**: Sensitive settings MUST use Secret Bindings rather than stored secret values.
- **FR-027**: Secret Bindings MUST be resolved through a pluggable resolver; database-owned secrets MUST integrate with Elsa Secrets and configuration-owned secrets MAY use deployment configuration providers.
- **FR-028**: Management and diagnostic surfaces MUST expose only secret configured state and support replacement or removal without reveal.
- **FR-029**: OpenID Connect discovery MUST be the default; selected overrides and fully manual Provider Trust Settings MAY be enabled by deployment policy.
- **FR-030**: Unsafe provider-trust overrides MUST require dedicated permission, explicit confirmation, persistent warning, and security notification.
- **FR-031**: Connection administrators MUST NOT weaken Elsa-owned Broker Security Invariants.

#### Broker, Clients, and Sessions

- **FR-032**: Elsa Server MUST own provider redirects, callbacks, account resolution, permission resolution, and Elsa credential issuance.
- **FR-033**: Provider tokens, Elsa tokens, and secrets MUST NOT appear in redirect URLs.
- **FR-034**: Successful local and external authentication MUST complete with a short-lived, single-use Elsa authorization code.
- **FR-035**: The completion code MUST be bound to an Authentication Client, exact callback URI, target tenant, and PKCE challenge.
- **FR-036**: Authentication Clients MUST be distinct from Elsa API Applications, grant no permissions, and be deployment-managed in v1.
- **FR-037**: Broker state MUST bind target tenant, immutable Connection ID, and material connection revision.
- **FR-038**: Callback, code exchange, and external refresh MUST reject disabled, archived, or materially changed connections using authoritative current state.
- **FR-039**: Material revision MUST cover adapter and trust settings, scope, secret binding identity or generation, Unlinked Identity Policy, and Permission Grant Sources; presentation-only changes MAY avoid invalidating a flow.
- **FR-040**: Correlation state, completion codes, and connection checks MUST work when requests cross Elsa nodes.
- **FR-041**: Anonymous discovery and initiation MUST verify the shared effective-registry version before returning or redirecting, so a completed database mutation cannot remain stale indefinitely on another node.
- **FR-042**: External Authentication Sessions MUST retain a bounded external claim and grant snapshot and enforce a configurable maximum age.
- **FR-043**: External refresh credentials MUST identify their External Authentication Session and check connection state, maximum age, and revocation.
- **FR-044**: External refresh MUST reuse the external snapshot while reevaluating current Elsa-owned user and role grants.
- **FR-045**: Existing local refresh behavior MUST remain distinguishable and compatible.
- **FR-046**: Disabling or archiving a connection MUST stop initiation, in-flight callback, and refresh; existing short-lived access tokens remain valid until expiry unless explicitly revoked.
- **FR-047**: Normal logout MUST end the Elsa session.
- **FR-048**: Connections that support Upstream Logout MUST offer Disabled, UserChoice, and Always modes, defaulting to Disabled.

#### Elsa Users, Links, and Permissions

- **FR-049**: Successful external authentication MUST resolve to an Elsa User before Elsa credentials are issued.
- **FR-050**: External Identity Links MUST be separate from users and keyed by target tenant, immutable Connection ID, validated issuer namespace, and stable subject.
- **FR-051**: Built-in behavior MUST NOT link by email, user name, or another mutable profile attribute.
- **FR-052**: One Elsa User MAY have multiple links and MAY exist without Local Credentials.
- **FR-053**: Credential-less users MUST contain no placeholder password material and local login MUST fail with the same public result as other invalid credentials.
- **FR-054**: JIT provisioning MUST use an atomic create-link-or-get-existing contract that reserves a globally unique Elsa user name without making mutable provider attributes identity keys.
- **FR-055**: The External Identity Link tuple `(target tenant, Connection ID, issuer namespace, subject)` MUST have durable uniqueness. Concurrent JIT or prelink operations for the same tuple MUST converge on one link/user; a generated-name collision MUST retry with another generated name or fail safely.
- **FR-056**: JIT-created users MUST belong to the broker-resolved target tenant. Prelinking and link resolution MUST reject a user whose tenant does not match that target.
- **FR-057**: The safe default Unlinked Identity Policy MUST reject access; v1 MUST also include an explicitly selectable JIT creation policy.
- **FR-058**: Deployment configuration MUST control the default policy, allowed policy types, and whether a database-owned connection may override it.
- **FR-059**: V1 MUST support administrator prelinking and unlinking but MUST NOT include end-user self-service linking.
- **FR-060**: Elsa MUST remain authoritative for the permission claims in Elsa credentials.
- **FR-061**: Permission Grant Sources MUST compose Elsa-owned user/role grants and explicitly configured external claim/group mappings.
- **FR-062**: External claims MUST grant nothing without an explicit mapping or pass-through boundary.
- **FR-063**: An actor configuring a grant MUST possess every permission that can be delegated unless they have separate unrestricted delegation authority; deployment allow/deny boundaries still apply.
- **FR-064**: Modules MAY advertise optional Permission Descriptors for authoring and warnings; unknown permission strings remain valid but grant access only where a module requires them.
- **FR-065**: Complete external claim sets and provider tokens MUST NOT be persisted by default.

#### Login Discovery and Studio

- **FR-066**: Login Method discovery MUST unify local Elsa credentials and external connections without modeling local login as a connection.
- **FR-067**: Anonymous discovery MUST return only method identifier/key, local-or-external kind, display name, trusted server-hosted icon identifier, display order, default state, tenant applicability already resolved by the server, and Elsa-owned initiation URL. It MUST NOT return adapter settings, provider authority, upstream client identifier, tenant internals, health, secrets, or remote icons.
- **FR-068**: Studio MUST show an accessible chooser by default and MAY automatically redirect to a scope-appropriate default external connection.
- **FR-069**: Automatic redirect MUST preserve an explicit chooser escape path, avoid loops, and return to the chooser on failure.
- **FR-070**: Local Login Method availability and default selection MUST be deployment-controlled and resolved by scope.
- **FR-071**: At most one automatic external default MAY be effective per Connection Scope. A tenant-specific default takes precedence over a host-wide default; an unavailable default falls back to the chooser, and no available normal method returns a safe unavailable state without exposing Break-glass Authentication.
- **FR-072**: Local credential authentication MUST use the same client-, callback-, tenant-, and PKCE-bound completion contract as external authentication.
- **FR-073**: Brokered local credential completion MUST use a new opt-in broker endpoint. The existing token-returning local login and refresh endpoints MUST retain their public contract for existing API clients.
- **FR-074**: Studio Server MUST act as a confidential Authentication Client, exchange the code on the host, retain refresh credentials server-side, and establish a secure HTTP-only browser session.
- **FR-075**: Studio WebAssembly MUST act as a public Authentication Client with no client secret and mandatory PKCE. Code exchange MUST return an Elsa access token and rotating, reuse-detecting external-session refresh token only to an exactly registered origin; wildcard origins and credentialed cross-origin requests are forbidden.
- **FR-076**: The default WebAssembly token accessor MUST keep post-exchange credentials in memory so reload, a new tab, or a closed tab requires sign-in. Tab-scoped session storage and durable browser storage MAY be explicit deployment choices; every persistent browser policy MUST carry a security warning.
- **FR-077**: Every callback and logout URI MUST match an exact client registration.
- **FR-078**: Every user-controlled post-authentication target MUST be an allowlisted client-local path; absolute, protocol-relative, and unregistered targets MUST be rejected.
- **FR-079**: Studio MUST label the connection's Upstream Client Registration separately from the deployment-owned Elsa Authentication Client.
- **FR-080**: Studio MUST provide Identity Provider Connections and External Identity Links pages under Security without requiring full user or role management.
- **FR-081**: Link administration MUST use a permission-guarded, tenant-scoped, paginated user lookup returning only minimal selection data.
- **FR-082**: Management APIs and Studio routes MUST enforce operation authorization independently of menu visibility.
- **FR-083**: Studio MUST explain source ownership, shadowing, archive, validity, enabled intent, and stale test results while showing only caller-allowed actions.
- **FR-084**: Login buttons MUST be text-first, keyboard and screen-reader accessible, deterministically ordered, and limited to trusted server-hosted presentation assets with safe fallback.

#### Security, Operations, and Recovery

- **FR-085**: Management operations MUST use distinct permissions for read, create, update, archive/restore, test, preview, policy, unsafe trust settings, permission delegation, link management, and session revocation.
- **FR-086**: Preview Sign-in MUST use separate short-lived, one-time state and result records bound to administrator, tenant, Connection ID, draft revision, and preview callback.
- **FR-087**: Preview MUST NOT create or link a user, issue a normal code or credential, or open a normal session.
- **FR-088**: Only the initiating authorized administrator with an active Studio session MAY read the one-time, allowlisted, redacted preview result.
- **FR-089**: On-demand tests MUST record tested revision and timestamp and become stale after material change; v1 MUST NOT require polling or health history.
- **FR-090**: The latest redacted on-demand test result MUST be stored in a shared observation store keyed by Connection ID and tested revision so every node can return the same latest observation. A material revision change MUST make it stale; historical observations need not be retained.
- **FR-091**: Elsa SHOULD expose the same test contract as a separately tagged, opt-in application health check that does not affect readiness by default.
- **FR-092**: Outbound provider traffic MUST apply deployment egress controls, HTTPS by default, bounded time and response size, controlled redirects, resolved-address checks, and safe exception handling.
- **FR-093**: Deployment policy MUST be able to deny private, loopback, link-local, reserved, or unapproved destinations and use an approved proxy.
- **FR-094**: Public broker errors MUST use documented safe categories and correlation identifiers without distinguishing unknown users from missing links.
- **FR-095**: Anonymous discovery, initiation, callback, and exchange MUST be rate-limited; state and codes MUST expire and be single-use.
- **FR-096**: Secrets, tokens, unrestricted claims, and provider response bodies MUST NOT appear in responses, redirects, logs, tests, preview, health details, or security notifications.
- **FR-097**: Each connection MUST define a normalized-claim projection that allowlists claim types usable by mappings and preview, permits only bounded string or string-array values, applies configurable count and size limits, records claim provenance, and redacts values according to descriptor policy. Claims outside the projection MUST be discarded after sign-in processing.
- **FR-098**: Every Secret Binding resolution MUST produce an opaque, nonreversible generation fingerprint. A changed fingerprint is a material revision and MUST reject an in-flight callback; the fingerprint MUST never be exposed through management or diagnostic surfaces.
- **FR-099**: Rate-limit partition keys, thresholds, and retry behavior, plus outbound timeout, redirect, response-size, DNS/rebinding, proxy, and destination defaults MUST be deployment-configurable with secure documented defaults and a conformance test matrix.
- **FR-100**: Elsa MUST publish typed, immutable, redacted security notifications for sign-in outcomes and privileged connection, policy, secret, test, preview, link, and session operations.
- **FR-101**: This feature MUST NOT require an audit store; durable history is owned by an optional subscriber.
- **FR-102**: A configurable final-login-path guard MUST prevent lockout unless an independent deployment-owned Break-glass Authentication method or explicitly privileged confirmed override exists.
- **FR-103**: Break-glass Authentication MUST remain outside normal Login Method discovery and keep management reachable without the connection being repaired.

#### Compatibility and Delivery

- **FR-104**: Existing direct Studio OpenID Connect MUST remain supported as a deployment-selected mode during Elsa 3 adoption.
- **FR-105**: Direct OpenID Connect and Brokered External Authentication MUST be mutually exclusive for one Studio host; ambiguous configuration MUST fail startup.
- **FR-106**: Migration guidance MUST map direct settings to a configuration-owned broker connection without silently moving secrets or changing mode.
- **FR-107**: V1 MUST support both Studio Server and Studio WebAssembly and deliver the paired Studio modules with the Core/server capability.
- **FR-108**: V1 MUST include configuration-first broker operation, optional persisted administration, and the enterprise security controls defined in this specification.

### Key Entities

- **Identity Provider Connection**: Elsa's trust relationship with an external provider, including immutable identity, scope-local key, source, scope, adapter selection, presentation, lifecycle state, revision, versioned settings, and Secret Bindings.
- **Protocol Adapter Description**: Safe metadata describing an installed adapter's settings, validation, secrets, capabilities, presentation, and schema version.
- **External Identity**: A normalized provider assertion identified by validated issuer namespace and stable subject.
- **External Identity Link**: A tenant-scoped association from connection/issuer/subject to the Elsa User that owns Elsa authorization.
- **Elsa User**: Elsa's authorization account, optionally possessing Local Credentials and potentially linked to multiple external identities.
- **Unlinked Identity Policy Selection**: The effective policy and settings used when an authenticated identity has no link.
- **Permission Grant Source Selection**: A configured contributor to Elsa permissions with explicit boundaries and provenance.
- **Authentication Client**: A deployment-owned Elsa client registration with client type, exact callbacks, PKCE requirement, and optional logout callbacks.
- **External Authentication Session**: A bounded session linking user, tenant, connection, external grant snapshot, maximum age, refresh eligibility, and revocation state.
- **Secret Binding**: A non-secret resolver type and lookup reference for sensitive connection values.
- **Connection Test Result**: A redacted observation bound to connection revision and timestamp, distinct from enablement and validity.
- **Preview Result**: A short-lived, one-time, administrator-bound redacted identity and authorization projection that cannot become a normal session.
- **Permission Descriptor**: Optional, non-authoritative module metadata for permission authoring and diagnostics.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An integrator can configure one OpenID Connect connection entirely through deployment configuration and complete external sign-in from both supported Studio hosts.
- **SC-002**: An authorized administrator can create and enable a persisted connection without restarting any Elsa node.
- **SC-003**: A conformance adapter with unique settings can be configured through the generic Studio editor without database schema changes or adapter-specific Studio code.
- **SC-004**: Automated contract tests find zero provider tokens, Elsa tokens, secret values, unrestricted claims, or provider response bodies in management, discovery, redirect, error, test, preview, health, log, and notification outputs.
- **SC-005**: Tenant-isolation tests show that discovery and links never reveal or resolve another tenant's scoped connection or user.
- **SC-006**: Multi-node tests complete initiation, callback, and code exchange on different nodes while preserving single-use and current-revision checks.
- **SC-007**: Disabling, archiving, materially changing, or revoking an external session prevents callback or refresh before any new Elsa credential is issued.
- **SC-008**: Permission tests prove unmapped claims grant nothing and ordinary administrators cannot delegate permissions they do not possess.
- **SC-009**: Credential-less users can authenticate externally, contain no placeholder password material, and receive indistinguishable local-login failure.
- **SC-010**: Preview tests prove no user, link, normal completion code, Elsa credential, or normal session is created.
- **SC-011**: Open-redirect, replay, tenant-enumeration, and outbound-request security tests reject all documented malicious cases.
- **SC-012**: Configuration/database collision and optimistic-concurrency tests produce deterministic, visible outcomes without silent overwrite.
- **SC-013**: Existing Direct OpenID Connect deployments continue unchanged until an explicit, validated mode switch.
- **SC-014**: Every privileged operation and sign-in outcome publishes a redacted security notification consumable by an audit subscriber.
- **SC-015**: The chooser and management paths pass automated keyboard, screen-reader semantics, and trusted-asset checks for the defined components.

## Assumptions

- The existing Elsa Identity user, role, permission claim, and token facilities remain the authorization foundation.
- The Elsa User credential model will be migrated compatibly to permit users without Local Credentials.
- Elsa Secrets or an equivalent resolver can provide writable secrets for persisted connections.
- Deployments that require cross-node sign-in provide shared protected state and compatible data-protection configuration.
- Authentication Clients, default policies, allowed extension types, unsafe-provider boundaries, break-glass methods, and Studio authentication mode remain deployment-controlled in v1.
- Database persistence for connection data is optional; configuration-first operation is a complete supported deployment.
- OpenID Connect is the only provider protocol delivered in v1; provider-specific OAuth and SAML adapters are extension follow-ups.
- Full user/role administration, end-user self-linking, continuous provider monitoring, audit persistence, provider-token retention, and connection import/export remain outside this feature.
