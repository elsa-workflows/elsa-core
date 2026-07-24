# PRD: External Authentication and Identity Provider Connections

**Status**: Draft for review

**Delivery boundary**: Elsa Core and Elsa Studio

**Initial protocol scope**: OpenID Connect

**Domain language**: [`CONTEXT.md`](../../CONTEXT.md)

**Architecture decisions**: [`docs/adr`](../../docs/adr)

## Summary

Add an External Authentication capability to Elsa 3 that lets administrators and deployers register Identity Provider Connections through either deployment configuration or optional database persistence. Elsa Server brokers external sign-in, resolves the authenticated External Identity to a tenant-scoped Elsa User, composes Elsa permissions, and issues Elsa credentials.

Elsa Studio will provide:

- A management experience for database-owned connections.
- A read-only view of configuration-owned connections.
- A unified login chooser for local Elsa credentials and enabled external connections.
- External Identity Link administration.
- Safe connection testing and interactive preview.

The first release supports OpenID Connect. The connection, adapter, policy, permission, and Studio contracts must allow future provider-specific OAuth adapters such as GitHub without changing the core data model or broker flow.

## Problem

Elsa currently supports local username/password authentication, Elsa-issued JWTs, and API keys. Elsa Studio also supports startup-configured direct OIDC authentication, but that implementation assumes one provider and does not support runtime provider administration.

Customers need to:

- Offer multiple external authentication choices.
- Add or change connections without restarting Elsa.
- Keep some connections deployment-controlled through configuration.
- Manage other connections from Elsa Studio.
- Preserve Elsa-specific authorization after external authentication.
- Extend the system with provider-specific adapters without adding database columns or rebuilding the management UI for every provider.
- Operate safely across tenants, Studio hosting models, and multi-node Elsa deployments.

Without a server-owned broker and a connection registry, Studio would need provider credentials and protocol-specific behavior, every client would implement account linking independently, and dynamically stored providers would not have a safe common execution model.

## Goals

- Make Elsa Server the single broker for external authentication.
- Support a merged registry of configuration-owned and database-owned connections.
- Let database-managed connection changes take effect without restart.
- Keep provider secrets outside connection records and read APIs.
- Support host-wide and tenant-scoped connections.
- Preserve Elsa Users, permissions, and tenant context as the authorization authority.
- Support external-only Elsa Users without generated local passwords.
- Provide extensible Protocol Adapters, Unlinked Identity Policies, and Permission Grant Sources.
- Provide a descriptor-driven Studio UI with optional custom editors.
- Unify local, enterprise, and future social authentication as Login Methods.
- Support both Blazor Server and Blazor WebAssembly Studio hosts.
- Preserve existing direct Studio OIDC behavior during adoption.
- Emit audit-ready security notifications without requiring an audit persistence module.

## Non-Goals

- SAML support in the first release.
- Provider-specific OAuth adapters in the first release.
- Self-service identity linking by end users.
- Full Elsa User and Role management UI.
- Studio management of Authentication Clients.
- An audit database, audit search UI, or retention engine.
- A health-monitoring scheduler or health-history store.
- Continuous provider token introspection.
- Persistence of complete external claim sets.
- Automatic migration of existing OIDC secrets or settings.
- Removal of Studio's existing direct OIDC modules.
- Import/export of Identity Provider Connections in the first release.

## Users

- **Deployment owner**: installs adapters, configures Authentication Clients, defines configuration-owned connections, secrets, defaults, and security guardrails.
- **Connection administrator**: creates and operates permitted database-owned connections in Studio.
- **Security administrator**: manages admission policies, permission sources, unsafe provider-trust overrides, identity links, and session revocation.
- **Operator**: reads and tests connections without necessarily being allowed to change them.
- **Elsa user**: chooses an applicable Login Method and receives Elsa permissions after successful authentication.
- **Module author**: contributes Protocol Adapters, policies, permission sources, permission descriptors, or optional custom Studio editors.

## Product Principles

1. **Elsa owns the broker**: Provider interaction, account resolution, and Elsa credential issuance happen on Elsa Server.
2. **External identity is not Elsa authorization**: External authentication resolves an Elsa User before Elsa permissions are issued.
3. **Code is deployed; connection data is live**: New adapter code may require deployment and restart. Connections for installed adapters do not.
4. **Safe by default, flexible by explicit choice**: Discovery, validation, and restrictive policies are defaults; authorized administrators may override provider-trust behavior where deployment policy permits.
5. **Broker protections are invariants**: Connection administrators cannot disable Elsa-owned callback, correlation, PKCE, one-time-code, or secret-redaction protections.
6. **No implicit account matching**: Email and user name never establish identity links by default.
7. **No implicit external authorization**: Provider claims grant Elsa permissions only through configured Permission Grant Sources and boundaries.
8. **Sources retain ownership**: Configuration-owned connections are deployer-owned and read-only in Studio.
9. **Operational state is explicit**: Enabled intent, validity, and observed health are separate concepts.
10. **Extension metadata is helpful, not authoritative**: Permission Descriptors improve authoring but do not define the validity of Elsa's open string permission vocabulary.

## Existing Product Surface

### Elsa Core

- Local login is exposed through the Elsa Identity module.
- Elsa Users carry local password hashes, roles, and tenant context.
- Elsa access-token issuance resolves role permissions and emits `permissions` claims.
- User and role CRUD APIs already exist.
- Configuration-based and store-based identity providers exist, but they are mutually exclusive rather than merged.
- JWT bearer and API-key authentication are supported; there is no existing external-login broker.
- Elsa Mediator provides `INotificationSender` for module notifications.
- ASP.NET Core health checks are already used by Elsa runtime modules.

### Elsa Studio

- Existing direct OIDC modules configure one provider at startup.
- Blazor Server and WebAssembly use different current authentication plumbing.
- The existing Security menu includes Users and Roles routes, but both pages are placeholders.
- Existing module, navigation, Refit client, CRUD page, dialog, and UI-hint patterns can support the new management experience.

## Target Experience

### Connection Administration

1. An authorized administrator opens **Security → Identity Provider Connections**.
2. Studio lists configuration-owned and database-owned connections together.
3. Source, tenant scope, adapter type, enabled state, validity, last observed health, default-login state, and revision are visible.
4. Configuration-owned connections are read-only.
5. The administrator creates a disabled database draft and selects an installed Protocol Adapter.
6. Studio renders common fields and adapter settings from the Adapter Descriptor.
7. Secret fields show configured state and allow replacement or removal without revealing values.
8. The administrator runs a non-interactive connection test and may run an interactive Preview Sign-in.
9. Preview shows a redacted normalized identity, tenant, policy decision, and effective Elsa permissions without provisioning a user or opening a normal session.
10. The administrator enables the connection after structural validation and required Secret Binding resolution succeed.
11. The enabled connection becomes available across the Elsa cluster without restart.

### User Sign-in

1. Studio resolves the target Elsa tenant.
2. Studio discovers the applicable Login Methods anonymously.
3. The chooser displays local Elsa login when enabled and applicable external connections.
4. If an administrator configured automatic redirect, Studio redirects to the default external connection while preserving a chooser escape route.
5. Studio initiates brokered sign-in using its registered Authentication Client and a PKCE challenge.
6. Elsa loads the selected enabled connection and its exact revision.
7. The Protocol Adapter performs provider authentication and returns a normalized External Identity.
8. Elsa resolves an External Identity Link by target tenant, immutable Connection ID, issuer namespace, and stable subject.
9. If no link exists, the connection's effective Unlinked Identity Policy denies access, creates a user, or returns an explicitly configured target-user resolution. A target-user resolution must identify the user and authorization basis; it may not silently infer a link from email or user name.
10. Elsa applies the result atomically, evaluates configured Permission Grant Sources, and establishes an External Authentication Session.
11. Elsa redirects Studio with a short-lived, single-use Elsa authorization code.
12. Studio exchanges the code using the PKCE verifier and establishes its Elsa session.

Local Elsa credentials use the same broker-completion contract: Studio submits credentials to an Elsa-owned local-login endpoint, Elsa validates them without exposing whether an account lacks a Local Credential, and successful authentication returns the same Authentication Client-, callback-, tenant-, and PKCE-bound completion code. Local authentication remains a Login Method, not an Identity Provider Connection.

### Studio Hosting Profiles

| Studio host | Authentication Client | Code exchange and callback owner | Browser/session outcome |
| --- | --- | --- | --- |
| Blazor Server | Confidential server client | The Studio server host owns the callback and exchanges the code with PKCE and its server-held client authentication | Provider and Elsa refresh credentials remain server-side; the browser receives only a secure, HTTP-only Studio session cookie |
| Blazor WebAssembly | Public browser client | The browser callback exchanges the code directly with PKCE; no client secret is issued or accepted | Elsa credentials are handled by the Studio token accessor, never placed in URLs, and use an explicit deployment storage policy whose safe default is in-memory only |

The broker must allow only exact registered callback and logout URIs. Public-client exchange must use an exact-origin CORS allowlist. A Studio host selects either **Direct OIDC** or **Brokered External Authentication** at deployment time; enabling both modes is a startup configuration error. Switching back to Direct OIDC is the supported rollback path.

All return targets—after local or external login, automatic-redirect recovery, chooser escape, preview, and logout—must resolve to an allowlisted client-local path. User-controlled absolute or protocol-relative URLs are rejected.

### Identity Link Administration

1. An authorized administrator opens the dedicated External Identity Links page.
2. The administrator selects an Elsa User.
3. Studio lists safe link metadata and last successful sign-in information.
4. The administrator may pre-link a tenant, connection, issuer namespace, and subject for pre-provisioned-only admission.
5. The administrator may unlink an identity after explicit confirmation.
6. External tokens and unrestricted claims are never shown.

The user picker uses a tenant-scoped, permission-guarded, paginated lookup that returns only the minimum display identity needed to select an Elsa User. It never returns credential fields.

### Recovery

1. Deployment owners configure an independent Break-glass Authentication method when lockout protection is enabled.
2. Elsa rejects disabling the final valid sign-in path unless the caller uses an explicitly privileged, confirmed override.
3. A deployment owner can use the break-glass method to repair a failed provider without authenticating through that provider.

## Release Milestones

### Milestone 1: Configuration-first Broker Foundation

- External authentication module and registries.
- OIDC Protocol Adapter.
- Configuration-owned connections.
- Host-wide and tenant-scoped registry semantics.
- Authentication Clients and exact callback allowlists.
- PKCE broker initiation and completion.
- External Identity normalization, linking, and Elsa User resolution.
- External-only Elsa Users.
- Built-in reject and JIT Unlinked Identity Policies.
- Permission Grant Source pipeline.
- Anonymous Login Method discovery.
- Studio login support for Blazor Server and WebAssembly.

### Milestone 2: Persisted Administration

- Optional connection persistence and merged connection registry.
- Secret Binding resolver and Elsa Secrets integration.
- Connection CRUD, archive/restore, enable/disable, and optimistic concurrency.
- Descriptor-driven Studio management UI.
- Non-interactive connection testing.
- Interactive Preview Sign-in.
- Enabled, validity, and observed-health presentation.
- Safe error handling and structured diagnostics.

### Milestone 3: Enterprise Hardening

- Tenant-isolation and collision hardening across host-wide and tenant-scoped connections.
- Cluster-wide invalidation and shared broker state.
- External Identity Link administration.
- Permission Descriptors and Studio permission authoring.
- Fine-grained administration permissions.
- Audit-ready security notifications.
- Break-glass and final-login-path guardrails.
- Unsafe Provider Trust Setting controls.
- Session refresh and revocation behavior.
- Compatibility and migration guidance.

V1 is complete when all three milestones meet their acceptance criteria.

## Functional Requirements

### Module and Extensibility

- **FR-001**: Elsa MUST provide an External Authentication module independent of Studio hosting.
- **FR-002**: Protocol Adapters MUST be registered as trusted server modules at startup.
- **FR-003**: Installing a new adapter MAY require deployment and restart.
- **FR-004**: Creating, updating, enabling, disabling, or archiving a connection for an installed adapter MUST NOT require restart.
- **FR-005**: Every adapter MUST expose a stable adapter type and an Adapter Descriptor.
- **FR-006**: Adapter Descriptors MUST describe settings, validation, secret fields, presentation, capabilities, and settings schema version.
- **FR-007**: Adapters MUST normalize successful authentication into a protocol-neutral External Identity.
- **FR-008**: Adapter-owned connection settings MUST be stored as versioned JSON within a protocol-neutral connection envelope.
- **FR-009**: Adapters MUST own settings deserialization, validation, and version migration.
- **FR-010**: Studio MUST render descriptor-defined settings with its standard UI-hint system.
- **FR-011**: A Studio module MAY register a custom editor for an adapter type and override the generic renderer.

### Connection Registry and Ownership

- **FR-012**: Elsa MUST expose one effective Connection Registry composed from configuration-owned and optional database-owned connections.
- **FR-013**: Configuration-owned connections MUST be visible but read-only through Studio and management APIs.
- **FR-014**: Database-owned connections MUST support create, read, update, enable, disable, archive, restore, and test operations.
- **FR-015**: Configuration-owned connections MUST take precedence over database entries with the same `(Connection Scope, Connection Key)`.
- **FR-016**: Database creation or key changes that collide with configuration MUST be rejected.
- **FR-017**: A later configuration collision MUST make the database entry inactive and visible as shadowed.
- **FR-018**: Every connection MUST have an immutable internal Connection ID.
- **FR-019**: Every connection MUST have a stable presentation key unique within each tenant's effective registry.
- **FR-020**: Different tenants MAY reuse the same connection key.
- **FR-021**: A tenant-scoped connection MUST NOT collide with an inherited host-wide connection in that tenant's effective registry.
- **FR-022**: Connection deletion MUST logically archive the connection, preserve identity links, and emit an audit-ready notification. Audit history is retained only when an external subscriber persists those notifications.
- **FR-023**: Restoring an archived connection MUST preserve its Connection ID.
- **FR-024**: A genuinely new connection MUST receive a new Connection ID and MUST NOT inherit archived identity links.
- **FR-025**: Database-managed mutations MUST use optimistic concurrency with a revision or ETag.
- **FR-026**: Material connection changes MUST advance the revision.

### Tenancy

- **FR-027**: Connections MUST support host-wide scope and tenant scope from the first data-model version. Host-wide scope MUST use Elsa's tenant-agnostic identifier (`*`); the empty identifier MUST continue to mean only Elsa's default tenant.
- **FR-028**: Anonymous discovery with a resolved tenant MUST return applicable tenant connections plus host-wide connections.
- **FR-029**: Anonymous discovery without a tenant MUST return host-wide connections only.
- **FR-030**: The target tenant MUST be resolved before external redirection and protected in broker state.
- **FR-031**: External Identity Links MUST include the target tenant so one provider identity can link to distinct tenant-scoped Elsa Users.

### Connection Settings and Secrets

- **FR-032**: Common connection fields MUST remain protocol-neutral.
- **FR-033**: Adapter-specific fields MUST NOT require new connection columns or tables.
- **FR-034**: Connection settings MUST contain Secret Bindings rather than secret values.
- **FR-035**: The module MUST resolve Secret Bindings through a pluggable resolver abstraction.
- **FR-036**: The initial implementation MUST integrate with Elsa Secrets for database-managed secret values.
- **FR-037**: Configuration-owned connections MAY resolve secrets through standard .NET configuration providers.
- **FR-038**: Management APIs MUST expose only secret configured state.
- **FR-039**: Studio MUST support secret replacement and removal but MUST NOT reveal current values.
- **FR-040**: Secrets, tokens, and unrestricted claims MUST NOT appear in responses, redirects, logs, health details, preview reports, or audit notifications.

### Connection Lifecycle

- **FR-041**: Disabled database connections MAY be saved as incomplete drafts.
- **FR-042**: Effective enablement MUST require adapter structural validation and resolution of required Secret Bindings.
- **FR-043**: Invalid configuration-owned connections MUST remain administratively visible but MUST NOT become effectively enabled.
- **FR-044**: Enabled state MUST represent administrative intent independently of observed health.
- **FR-045**: Provider health failures MUST NOT automatically disable or hide a structurally valid enabled connection.
- **FR-046**: Studio MUST expose on-demand connection testing with redacted results.
- **FR-047**: The module SHOULD offer an opt-in, separately tagged ASP.NET Core health check using the same adapter test contract.
- **FR-048**: V1 MUST NOT require continuous polling, health-history persistence, or a monitoring UI.
- **FR-049**: Authorized administrators MUST be able to Preview Sign-in against a disabled draft revision.
- **FR-050**: Preview MUST NOT create/link a user, issue a completion code, issue Elsa credentials, or open a normal session.

### OIDC Adapter

- **FR-051**: V1 MUST include an OpenID Connect Protocol Adapter.
- **FR-052**: Discovery mode MUST be the safe default.
- **FR-053**: The OIDC adapter MUST support discovery, discovery with selected overrides, and fully manual Provider Trust Settings.
- **FR-054**: Studio MUST clearly label unsafe settings, require a dedicated permission and confirmation, show persistent warnings, and emit audit notifications.
- **FR-055**: Deployment policy MUST be able to restrict unsafe settings.
- **FR-056**: Connection administrators MUST NOT be able to weaken Broker Security Invariants.
- **FR-057**: The adapter MUST support standard authorization-code authentication, provider-facing PKCE where configured, issuer/signature/nonce/state validation according to effective Provider Trust Settings, and declared upstream-logout capability.
- **FR-058**: Provider-specific OAuth and SAML adapters MUST be addable later without changing the connection envelope or broker completion protocol.

### Broker and Authentication Clients

- **FR-059**: Elsa Server MUST own provider redirection, callback processing, account resolution, permission resolution, and Elsa credential issuance.
- **FR-060**: Elsa MUST NOT return provider tokens or Elsa tokens in redirect URLs.
- **FR-061**: External completion MUST use a short-lived, single-use Elsa authorization code.
- **FR-062**: The completion code MUST be bound to an Authentication Client, exact callback URI, target tenant, and PKCE challenge.
- **FR-063**: Authentication Clients MUST be distinct from Elsa API Applications and MUST grant no Elsa permissions.
- **FR-064**: V1 Authentication Clients MUST be deployment-configured rather than Studio-managed.
- **FR-065**: Broker state MUST include the immutable Connection ID and material revision.
- **FR-066**: Callback processing MUST reject a flow when the connection was disabled, archived, or materially revised after initiation.
- **FR-067**: Broker state, PKCE material, correlation state, and completion codes MUST work when initiation and completion occur on different Elsa nodes.
- **FR-068**: Material revision MUST cover adapter type and settings, Provider Trust Settings, Connection Scope, Secret Binding identity or generation, Unlinked Identity Policy, and Permission Grant Source configuration. Display name, icon, and display order MAY use a presentation-only revision that does not invalidate an in-flight flow.
- **FR-069**: Initiation, callback, code exchange, and external-session refresh MUST verify authoritative enabled, archive, and effective material-revision state. Cache invalidation MAY improve freshness but MUST NOT be the security boundary.

### Elsa Users and Identity Links

- **FR-070**: Successful external authentication MUST resolve to an Elsa User before Elsa credentials are issued.
- **FR-071**: External Identity Links MUST be separate from Elsa Users.
- **FR-072**: One Elsa User MAY have multiple External Identity Links.
- **FR-073**: An Elsa User MAY exist without Local Credentials.
- **FR-074**: JIT provisioning MUST NOT generate placeholder passwords.
- **FR-075**: Elsa's User persistence model MUST be migrated so Local Credentials are absent or separate rather than represented by placeholder password hashes.
- **FR-076**: Local login for a credential-less user MUST fail with the same public result as other invalid credentials.
- **FR-077**: JIT provisioning MUST atomically create a globally unique Elsa user name under the current identity-store contract; mutable provider profile attributes MUST NOT become identity keys. A future tenant-scoped user-name migration is outside this feature unless separately specified.
- **FR-078**: External Identity Links MUST be resolved by target tenant, immutable Connection ID, validated issuer namespace, and provider-stable subject.
- **FR-079**: Built-in behavior MUST NOT link by email or user name.
- **FR-080**: A custom Unlinked Identity Policy MAY deliberately implement deployment-specific linking behavior.
- **FR-081**: V1 MUST provide built-in reject and just-in-time creation policies.
- **FR-082**: Custom Unlinked Identity Policies MUST be deployable trusted modules with versioned settings descriptors.
- **FR-083**: Deployment configuration MUST define the default policy, allowed policy types, and whether database connections may override it.
- **FR-084**: Studio MUST show the effective policy and permit overrides only when deployment policy and caller permissions allow.
- **FR-085**: V1 MUST support administrator-managed pre-linking and unlinking.
- **FR-086**: V1 MUST NOT support end-user self-service linking.
- **FR-087**: Complete external claim sets MUST NOT be persisted by default.

### Permission Resolution

- **FR-088**: Elsa MUST remain authoritative for the `permissions` claims placed in Elsa-issued credentials.
- **FR-089**: Effective permissions MUST be composed from configured Permission Grant Sources.
- **FR-090**: V1 MUST support Elsa User role permissions as a grant source.
- **FR-091**: V1 MUST support explicit external claim-to-permission and external group-to-permission mappings.
- **FR-092**: Provider claims MUST NOT grant permissions without an explicit mapping or pass-through boundary.
- **FR-093**: External pass-through MUST grant nothing when no permission boundary is configured.
- **FR-094**: Permission Grant Sources MUST be extensible through trusted deployed modules and descriptor-defined settings.
- **FR-095**: An actor configuring literal, wildcard, or pass-through grants MUST possess every permission that can be delegated, unless the actor holds a distinct unrestricted permission-delegation capability. Deployment allow/deny boundaries MUST apply even to that capability.
- **FR-096**: Modules MAY advertise Permission Descriptors containing names, descriptions, and categories.
- **FR-097**: Permission Descriptors MUST be optional and non-authoritative.
- **FR-098**: Unknown permission strings MUST remain valid values but match no endpoint unless a module requires them.
- **FR-099**: External claims and effective external grants MUST be snapshotted at full external sign-in.
- **FR-100**: Elsa token refreshes within the same External Authentication Session MUST retain that external snapshot.
- **FR-101**: External refresh credentials MUST reference an External Authentication Session and MUST check its connection, maximum age, and revocation state; existing local refresh credentials MUST remain distinguishable and compatible.
- **FR-102**: On external-session refresh, external mapped grants MUST come from the session snapshot while current Elsa-owned user and role grants MUST be re-evaluated.
- **FR-103**: A configurable maximum external session age MUST require fresh provider authentication.
- **FR-104**: V1 MUST NOT retain provider tokens solely to continuously re-query external claims.

### Login Discovery and Studio

- **FR-105**: Elsa MUST expose anonymous tenant-aware Login Method discovery.
- **FR-106**: Login Methods MUST unify local Elsa credentials and external connections without modeling local login as an Identity Provider Connection.
- **FR-107**: Anonymous discovery MUST return only method identifier/key, local-or-external kind, display name, server-hosted icon identifier, display order, default state, and Elsa-owned initiation URL.
- **FR-108**: Anonymous discovery MUST NOT expose adapter settings, authority, client ID, tenant internals, health details, or remote icon URLs.
- **FR-109**: Studio MUST show a chooser by default.
- **FR-110**: Administrators MAY select one external connection for automatic redirect.
- **FR-111**: Local Login Method availability and automatic-default selection MUST be deployment-controlled and resolved per Connection Scope. Tenant-specific configuration takes precedence over host-wide defaults without exposing tenant existence.
- **FR-112**: Automatic redirect MUST fall back to the chooser on failure and MUST provide an explicit chooser escape URL.
- **FR-113**: The initial Studio module MUST support Blazor Server and Blazor WebAssembly.
- **FR-114**: Blazor Server MUST use a confidential Authentication Client; the Studio host MUST perform exchange, keep Elsa refresh credentials server-side, and establish a secure HTTP-only browser session.
- **FR-115**: Blazor WebAssembly MUST use a public Authentication Client with no client secret, mandatory PKCE, exact-origin CORS, and an explicit token-storage policy that defaults to in-memory storage.
- **FR-116**: Studio MUST distinguish the connection's Upstream Client Registration from the deployment-owned Elsa Authentication Client in labels, help text, validation, and prerequisites.
- **FR-117**: Studio MUST provide a dedicated External Identity Links page with an Elsa User picker and a reusable user-link panel.
- **FR-118**: Link administration MUST include a tenant-scoped, permission-guarded, paginated Elsa User lookup returning only minimal selection data.
- **FR-119**: Full user and role CRUD UI MUST remain outside this feature.

### Logout and Session Control

- **FR-120**: Normal logout MUST end the Elsa session.
- **FR-121**: Connections whose adapters support Upstream Logout MUST expose `Disabled`, `UserChoice`, and `Always` modes.
- **FR-122**: Upstream Logout MUST default to `Disabled`.
- **FR-123**: Disabling or archiving a connection MUST reject new sign-ins, in-flight callbacks, and further Elsa token refreshes associated with it.
- **FR-124**: Existing short-lived access tokens MUST remain valid until expiry unless an explicit supported revocation action invalidates them.
- **FR-125**: Session revocation MUST be a separate audited action when server-side revocation support is enabled.

### Administration, Recovery, and Audit

- **FR-126**: Connection operations MUST use dedicated Elsa permissions for read, create, update, archive/restore, test, policy management, unsafe security overrides, identity-link management, and session revocation.
- **FR-127**: Configuration-owned connections MUST remain immutable through runtime APIs regardless of caller permissions.
- **FR-128**: Elsa MUST support a configurable final-login-path lockout guard.
- **FR-129**: When the guard is active, disabling the final valid sign-in path MUST require a deployment-owned Break-glass Authentication method or an explicitly privileged confirmed override.
- **FR-130**: Break-glass Authentication MUST NOT appear in normal Login Method discovery.
- **FR-131**: The module MUST publish typed, immutable, redacted security notifications through `INotificationSender`.
- **FR-132**: Notifications MUST cover connection and policy changes, secret replacement/removal, enable/disable/archive/restore, tests, previews, link changes, session revocation, and sign-in outcomes.
- **FR-133**: Notifications SHOULD contain actor, tenant, Connection ID, Elsa User ID when known, timestamp, outcome, correlation ID, and a redacted change summary.
- **FR-134**: The module MUST NOT require or own an audit persistence store.

### Errors and Abuse Protection

- **FR-135**: Broker failures returned to clients MUST use a documented stable set of safe error categories plus a correlation ID.
- **FR-136**: Provider response details MUST remain in redacted server diagnostics and security notifications.
- **FR-137**: Public errors MUST not distinguish unknown users from missing links.
- **FR-138**: Anonymous discovery, initiation, callback, and code-exchange endpoints MUST integrate with ASP.NET Core rate limiting.
- **FR-139**: State and completion codes MUST have strict expiration and single-use semantics.

### Hosted Client, Preview, and Management Safety

- **FR-140**: Local credential authentication MUST complete through the same Authentication Client-, callback-, tenant-, and PKCE-bound code contract as external authentication.
- **FR-141**: Every user-controlled return target MUST be validated as an allowlisted client-local path; absolute, protocol-relative, and unregistered targets MUST be rejected.
- **FR-142**: A Studio deployment MUST select either Direct OIDC or Brokered External Authentication. Startup MUST reject ambiguous mixed-mode configuration and migration guidance MUST document rollback.
- **FR-143**: When tenant context is absent, Studio MUST show only host-wide Login Methods and MUST NOT offer an anonymous tenant picker or reveal whether a tenant exists. Deployments MAY establish tenant context through a trusted host, route, invitation, or preconfigured client context.
- **FR-144**: Adapter outbound HTTP used for discovery, testing, preview, and callbacks MUST apply deployment egress policy, HTTPS-by-default, bounded time and response size, controlled redirects, DNS and resolved-address checks, and redacted exception handling.
- **FR-145**: Deployment policy MUST be able to deny private, loopback, link-local, reserved, or unapproved provider destinations and to route adapter traffic through an approved proxy.
- **FR-146**: Preview MUST use separate short-lived, one-time state and result records bound to administrator, tenant, Connection ID, draft revision, and preview callback.
- **FR-147**: Preview results MUST use an explicit field allowlist, be readable once by the initiating authorized administrator, and be discarded if that administrator's Studio session is lost or expires.
- **FR-148**: Management APIs and Studio routes MUST enforce operation permissions independently of menu visibility. The UI MUST accurately disable or hide unauthorized actions without treating that presentation as the security boundary.
- **FR-149**: Studio MUST explain configuration-owned, shadowed, archived, invalid, and stale-test states and show only the actions valid for the caller and current revision.
- **FR-150**: A last observed connection result MUST be labeled as an on-demand test with timestamp and tested revision; it MUST become stale after material change and MUST NOT be presented as continuous health.
- **FR-151**: Login Method buttons MUST be text-first, keyboard and screen-reader accessible, use trusted server-hosted assets with a safe fallback, and apply deterministic ordering. Display names and assets MUST be validated to reduce login-page spoofing.
- **FR-152**: External-authentication management MUST remain reachable through an independent local or Break-glass Authentication path when configured; it MUST NOT depend on the connection being repaired.

### Compatibility

- **FR-153**: Existing direct Studio OIDC modules MUST remain supported during Elsa 3 adoption.
- **FR-154**: Server-brokered External Authentication MUST be the recommended path for multiple and runtime-managed providers.
- **FR-155**: Migration documentation MUST map existing direct OIDC settings to one configuration-owned broker connection.
- **FR-156**: Migration MUST NOT silently move client secrets or change authentication mode.

## Conceptual Data Model

| Concept | Purpose | Important characteristics |
| --- | --- | --- |
| Identity Provider Connection | Elsa's trust relationship with an external provider | Immutable ID, key unique within Connection Scope, source, tenant scope (`*` for host-wide; empty for default tenant), adapter type, common presentation, enabled/archive state, revision, versioned settings, secret bindings |
| Adapter Descriptor | Describes an installed adapter | Type, version, fields, validation, UI hints, secret fields, capabilities, optional custom editor key |
| External Identity Link | Associates external identity with Elsa authorization | Target tenant, Connection ID, issuer, subject, Elsa User ID, timestamps |
| Elsa User | Owns Elsa authorization | Tenant context, optional Local Credential, roles and other Elsa-specific data |
| Unlinked Identity Policy Selection | Decides what to do with an unknown identity | Policy type, settings version, settings, inherited or connection override |
| Permission Grant Source Selection | Contributes Elsa permissions | Source type, settings version, settings, explicit boundaries |
| Authentication Client | Safe broker return target | Client ID, exact callback URIs, client type, PKCE requirement, optional logout callbacks |
| External Authentication Session | Bounded Elsa session created from external claims | User, tenant, connection, claim/grant snapshot, maximum age, refresh/revocation state |
| Secret Binding | Resolves sensitive adapter values | Resolver type and non-secret lookup reference |
| Permission Descriptor | Optional permission authoring metadata | Permission name, description, category, contributing module |

## Management Capabilities

The specification should define exact routes and schemas for:

- Connection list, detail, create, update, enable, disable, archive, and restore.
- Connection shadow/conflict reporting.
- Installed Adapter Descriptor discovery.
- Effective policy and Permission Grant Source descriptor discovery.
- Secret configured state, replacement, and removal.
- Structural validation and non-interactive testing.
- Interactive Preview Sign-in initiation and redacted result retrieval.
- Identity Link list, create, and unlink, plus bounded tenant-scoped user lookup.
- Permission Descriptor discovery.
- Session list/revocation where enabled.
- Anonymous Login Method discovery.
- Local-login initiation and normalized completion.
- Broker initiation, provider callback, completion-code exchange, logout, and optional Upstream Logout.

## Studio Information Architecture

```text
Security
├── Identity Provider Connections
│   ├── Connections
│   ├── Create/Edit
│   ├── Test
│   └── Preview Sign-in
├── External Identity Links
└── Future
    ├── Users
    └── Roles
```

The connection list should support filtering by source, scope, adapter type, enabled state, validity, shadow/conflict state, and archived state.

Configuration-owned connections are inspect-only. Every page and API independently enforces authorization; menu visibility is only an affordance. The connection editor labels provider-issued fields as **Upstream Client Registration** and displays the eligible deployment-owned **Elsa Authentication Client** only as a redacted prerequisite.

## Security and Privacy Requirements

- Use exact callback URI matching.
- Protect broker state against tampering and replay.
- Use PKCE for Elsa's completion-code handoff.
- Keep provider-facing protocol security under adapter control while preserving Broker Security Invariants.
- Never put tokens or secrets in URLs.
- Reject open redirects by allowing only registered callbacks and client-local return paths.
- Never reveal stored secrets.
- Never auto-link by mutable profile attributes.
- Never trust external permission claims without explicit configuration.
- Require dedicated permissions and audit notifications for unsafe settings and privileged actions.
- Avoid account enumeration in public errors.
- Do not persist complete external claim sets.
- Keep remote assets off the anonymous login page.
- Constrain outbound provider traffic to the deployment's egress and destination policy.
- Apply bounded lifetimes to state, completion codes, access tokens, refresh ability, and external claim snapshots.

## Operational Requirements

- Database-managed changes must propagate across the cluster without restart.
- Security decisions at initiation, callback, exchange, and refresh must verify authoritative current state rather than depend only on cache propagation.
- Shared broker state must allow callbacks and code exchange on any node.
- Configuration-owned changes may require deployment/restart.
- Health failures must not auto-disable connections.
- Provider outages must not make Elsa unready or trigger restart loops by default.
- Connection changes must emit redacted diagnostics and security notifications with correlation IDs.
- Disabling or archiving must stop new authentication and refresh before existing short-lived access tokens expire.

## Acceptance Scenarios

### A. Configuration-owned OIDC connection

Given a deployment-defined enabled OIDC connection, when Studio discovers Login Methods, then the connection appears read-only and a user can complete brokered sign-in without provider credentials being exposed to Studio.

### B. Database-owned connection lifecycle

Given an installed OIDC adapter and writable persistence, when an authorized administrator creates a disabled draft, supplies settings and a Secret Binding, previews sign-in, and enables it, then the method becomes available without restarting any Elsa node.

### C. Configuration/database collision

Given a database connection and a later configuration connection with the same effective key, when the registry resolves connections, then configuration wins and Studio marks the database record shadowed rather than silently using it.

### D. JIT external-only user

Given a successful external identity with no link and an effective JIT policy, when the broker completes sign-in, then Elsa creates an Elsa User without local password material, creates the tenant-scoped link, composes permissions, and issues Elsa credentials.

### E. Pre-provisioned-only user

Given an effective reject-unlinked policy, when an unlinked identity authenticates, then Elsa returns a safe denial. After an authorized administrator pre-links the identity, the same external sign-in succeeds.

### F. Permission grants

Given local Elsa role permissions plus explicitly mapped external claims, when sign-in completes, then the Elsa token contains their allowed effective union and contains no unbounded provider permissions.

### G. Tenant isolation

Given one host-wide connection and two tenant contexts, when the same external subject signs in to each tenant, then Elsa resolves distinct tenant-scoped links and users without exposing tenant-local connections across tenants.

### H. Connection revision change

Given a sign-in initiated on revision 4, when an administrator materially updates or disables the connection before callback, then callback rejects the flow with a safe retry error and does not complete against the new revision.

### I. Cluster callback

Given a sign-in initiated on node A, when the provider callback reaches node B, then shared protected state allows safe completion and the code remains single-use.

### J. Secret confidentiality

Given a configured client secret, when callers list, read, edit, test, preview, or audit the connection, then they can determine only whether the secret is configured and never receive its value.

### K. Unsafe provider-trust override

Given deployment policy permits unsafe overrides and the caller has the dedicated permission, when the caller confirms an override, then Elsa applies it, displays a persistent warning, and emits a redacted security notification without weakening Broker Security Invariants.

### L. Disabled or archived connection

Given an active external session, when its connection is disabled or archived, then new initiation, in-flight callback, and refresh fail while existing access tokens follow their configured short expiry.

### M. Upstream logout

Given an adapter supports upstream logout, when the connection mode is `UserChoice`, then Studio offers local logout and a separate provider logout action. When mode is `Always`, normal logout also initiates provider logout.

### N. Automatic redirect failure

Given an automatic default connection, when external authentication fails, then Studio returns to the chooser with a safe message and correlation ID and does not loop.

### O. Administrator lockout

Given final-login-path protection is active and no verified recovery path or privileged override is present, when an administrator attempts to disable the last valid method, then Elsa rejects the operation.

### P. Compatibility

Given an existing Studio direct OIDC deployment, when the new module is introduced but not selected, then existing authentication continues unchanged.

### Q. Studio hosting profiles

Given a Blazor Server client, when brokered login completes, then the server exchanges the code and the browser receives only an HTTP-only Studio session. Given a Blazor WebAssembly public client, then no client secret is accepted, PKCE and exact-origin CORS are enforced, and no credential appears in a URL.

### R. Redirect safety

Given an attacker supplies an absolute or protocol-relative return target to login, chooser recovery, preview, or logout, when Elsa or Studio validates it, then the target is rejected rather than followed.

### S. Permission delegation boundary

Given a connection administrator lacks an Elsa permission and lacks unrestricted permission delegation, when they map any external claim, group, wildcard, or pass-through source that could grant it, then Elsa rejects the change.

### T. External session refresh

Given an external session, when mapped provider claims change upstream without a fresh external sign-in, then refresh retains the external snapshot but re-evaluates current Elsa-owned role grants. When the session is revoked, its connection is disabled, or its maximum age is exceeded, refresh fails without changing local-session refresh behavior.

### U. Preview isolation

Given an administrator previews a disabled draft, when the preview callback completes, then only the initiating active administrator can read the one-time allowlisted result; no link, user, normal code, Elsa credential, or normal session is created.

### V. Authentication-mode conflict

Given Direct OIDC and Brokered External Authentication are both selected for one Studio host, when the host starts, then startup fails with a configuration error. Restoring the Direct OIDC selection provides the documented rollback.

### W. Unknown tenant context

Given an anonymous Studio client has no trusted tenant context, when it discovers Login Methods, then only host-wide methods are returned and no tenant names or existence signals are exposed.

### X. Credential-less user

Given an external-only Elsa User has no Local Credential, when local login is attempted, then it fails like any invalid credential. External sign-in and tenant-scoped identity resolution continue to work.

## Success Criteria

- An integrator can configure one OIDC connection entirely through deployment configuration.
- An authorized administrator can create and enable a persisted OIDC connection without server restart.
- Studio Server and WebAssembly can both complete the same brokered PKCE flow.
- Blazor Server keeps refresh credentials server-side; WebAssembly operates as a public client without a secret.
- A new adapter can add provider-specific fields without database schema changes.
- A new adapter without custom Studio code remains configurable through its descriptor.
- External-only users can authenticate and receive Elsa permissions.
- No test, management, preview, discovery, redirect, error, log, or notification path reveals secrets or tokens.
- Tenant-scoped login discovery and links remain isolated.
- Ordinary connection administrators cannot delegate Elsa permissions they do not possess.
- Sign-in initiation and completion work across nodes.
- Existing direct OIDC deployments remain supported.

## Dependencies

- Elsa Identity for users, roles, permission claims, and token issuance.
- A revised local-credential model that permits credential-less users.
- Optional connection persistence integrations.
- Elsa Secrets integration or another writable Secret Binding resolver.
- Shared protected state suitable for multi-node broker correlation and one-time codes.
- Elsa Mediator for security notifications.
- ASP.NET Core authentication, data protection, rate limiting, and optional health checks.
- Elsa Studio UI-hint, module, navigation, remote API client, and authentication abstractions.

## Risks

- The current User model requires password hashes and needs a compatible migration.
- Stateless existing refresh-token behavior may need extension to stop refresh by connection and enforce external-session age.
- Multi-node deployments require correctly shared data protection and transient broker state.
- Unsafe provider-trust overrides can enable impersonation when granted too broadly.
- Provider discovery and test endpoints can become SSRF paths unless outbound traffic follows deployment egress policy.
- Tenant resolution before login must avoid tenant enumeration and cross-tenant connection disclosure.
- Permission-source flexibility can become difficult to reason about without clear provenance and preview diagnostics.
- The current Studio Security pages are placeholders, so link administration needs its own user picker and page.
- Configuration and database merging must produce deterministic conflict diagnostics.

## Specification and Planning Follow-up

After PRD approval:

1. Run SpecKit specification generation using this PRD, `CONTEXT.md`, and the ADRs as sources.
2. Clarify and analyze the specification for contradictions, missing acceptance cases, and cross-repository impact.
3. Produce an implementation plan with research, data model, state machines, API contracts, Studio contracts, migrations, and threat modeling.
4. Generate story-oriented tasks with explicit Elsa Core and Elsa Studio ownership.
5. Convert reviewed tasks to GitHub issues.

The specification and plan must resolve:

- Package, feature, and public API names.
- Exact endpoint routes, payloads, error codes, and status codes.
- Exact connection settings envelope and schema-version migration contract.
- Persistence schemas and migration strategy for users, links, sessions, connections, and revisions.
- Distributed state implementation and propagation service-level target.
- Token, state, code, and external-session default lifetimes.
- Rate-limit defaults and partition keys.
- Permission names and descriptor contracts.
- Exact preview isolation and redaction behavior.
- Direct OIDC versus brokered-mode startup selection and rollback.
- Local-login request, failure, and broker-completion contracts.
- Browser credential storage and refresh behavior for the WebAssembly public-client profile.
- Connection test semantics per adapter.
- Session revocation implementation.
- Studio component contracts and callback handling for both hosting models.
