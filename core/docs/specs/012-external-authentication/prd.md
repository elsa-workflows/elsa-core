# PRD: External Authentication and Identity Provider Connections

**Status**: Approved — revised 2026-07-24

**Delivery boundary**: Elsa Core and Elsa Studio

**Initial protocol scope**: Host-wide, brokered OpenID Connect

**Domain language**: [`CONTEXT.md`](../../CONTEXT.md)

**Architecture decisions**: [`doc/adr`](../../doc/adr)

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
- Make SSO connection administration host-wide within the currently connected Elsa server environment without persisting an environment/target field.
- Preserve Elsa Users, permissions, and tenant context as the authorization authority.
- Support external-only Elsa Users without generated local passwords.
- Provide extensible Protocol Adapters, Unlinked Identity Policies, and External User Matchers.
- Provide a descriptor-driven Studio UI with optional custom editors.
- Unify local, enterprise, and future social authentication as Login Methods without automatically redirecting past the chooser.
- Support both Blazor Server and Blazor WebAssembly Studio hosts.
- Preserve existing direct Studio OIDC behavior during adoption.
- Emit audit-ready security notifications without requiring an audit persistence module.

## Non-Goals

- SAML support in the first release.
- Provider-specific OAuth adapters in the first release.
- Self-service identity linking by end users.
- Full Elsa User and Role management UI. This feature supplies the backend Role-deletion guard and remediation contract only; a future Roles UI MAY integrate it, but External Authentication MUST NOT add a separate Settings page for Role deletion.
- Studio management of Authentication Clients.
- An audit database, audit search UI, or retention engine.
- A health-monitoring scheduler or health-history store.
- Continuous provider token introspection.
- Persistence of complete external claim sets.
- Automatic migration of existing OIDC secrets or settings.
- Removal of Studio's existing direct OIDC modules.
- Import/export of Identity Provider Connections in the first release.
- Tenant-targeted Identity Provider Connections in the first release.
- IdP-initiated sign-in or logout.
- Claim-to-permission, group-to-permission, wildcard, or permission pass-through configuration in Studio.

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
4. **Safe by default, flexible by explicit choice**: Exact discovery documents and restrictive policies are defaults; adapters may expose safe protocol-specific settings without making broker invariants editable.
5. **Broker protections are invariants**: Connection administrators cannot edit Elsa-owned callback derivation, correlation, S256 PKCE, one-time-code, or secret-redaction protections.
6. **No implicit account matching**: Email and user name never establish identity links by default.
7. **No external permission authority**: External User Matchers may propose an existing user but never roles or permissions; Elsa alone expands Roles into permissions.
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

1. An authorized administrator opens **Settings → SSO** at `/settings/sso-connections`.
2. Studio lists configuration-owned connections, Studio-owned connections, and explicit Studio Overrides for the currently connected Elsa server environment.
3. Record ID, logical Connection Key, source, override relationship, adapter type, enabled state, validity, latest on-demand test, preferred-login state, and revision are visible.
4. Configuration-owned connections are read-only until the administrator explicitly creates a complete Studio Override.
5. The administrator creates a disabled Studio-owned draft or an explicit full-shadow override and selects an installed Protocol Adapter.
6. Studio renders common fields and adapter settings from the Adapter Descriptor.
7. Secret fields distinguish Managed Secrets from External Secrets. Managed values can be replaced or removed without reveal; External values remain deployment-owned and expose only configured/resolvable state.
8. The administrator runs a non-interactive connection test and may run an interactive Preview Sign-in.
9. Preview shows a redacted normalized identity, tenant, policy decision, and effective Elsa permissions without provisioning a user or opening a normal session.
10. The administrator enables the connection after structural validation and required Secret Binding resolution succeed.
11. The enabled connection becomes available across the Elsa cluster without restart.

### User Sign-in

1. Studio requests the Login Methods applicable to the deployment.
2. Elsa returns the environment's host-wide enabled methods.
3. The chooser displays local Elsa login when enabled and applicable external connections.
4. A preferred method is ordered and emphasized, but the chooser remains visible and never automatically redirects.
5. Studio initiates brokered sign-in using its registered Authentication Client and a PKCE challenge.
6. Elsa resolves the selected Login Method to its connection record ID and exact material revision.
7. The Protocol Adapter performs provider authentication and returns a normalized External Identity.
8. Elsa resolves an External Identity Link by immutable Connection Key, issuer namespace, and stable subject.
9. If no link exists, the connection's effective Unlinked Identity Policy denies access, creates a user, or returns an explicitly configured target-user resolution. A target-user resolution must identify the user and authorization basis; it may not silently infer a link from email or user name.
10. The Unlinked Identity Policy rejects, creates a user with static authorized `defaultRoleIds`, or invokes one configured External User Matcher to propose an existing user. Claims required for matching remain ephemeral.
11. Elsa redirects Studio with a short-lived, single-use Elsa authorization code.
12. Studio exchanges the code using the PKCE verifier and establishes its Elsa session.

Local Elsa credentials use the same broker-completion contract: Studio submits credentials to an Elsa-owned local-login endpoint, Elsa validates them without exposing whether an account lacks a Local Credential, and successful authentication returns the same Authentication Client-, callback-, tenant-, and PKCE-bound completion code. Local authentication remains a Login Method, not an Identity Provider Connection.

The v1 OpenID Connect adapter accepts one exact HTTPS `discoveryUrl`, uses a deployment-derived provider callback, requires a confidential upstream client and authorization-code flow with S256 PKCE, and supports `client_secret_basic` or `client_secret_post`. The normal path trusts discovery. An authorized administrator may open **Advanced** and explicitly override discovery-derived issuer, endpoints, or signing keys when deployment policy permits; these settings require warning, confirmation, and notification. Callback derivation and all broker validation invariants remain immutable.

### Studio Hosting Profiles

| Studio host | Authentication Client | Code exchange and callback owner | Browser/session outcome |
| --- | --- | --- | --- |
| Blazor Server | Confidential server client | The Studio server host owns the callback and exchanges the code with PKCE and its server-held client authentication | Provider and Elsa refresh credentials remain server-side; the browser receives only a secure, HTTP-only Studio session cookie |
| Blazor WebAssembly | Public browser client | The browser callback exchanges the code directly with PKCE; no client secret is issued or accepted | Elsa credentials are handled by the Studio token accessor, never placed in URLs, and use an explicit deployment storage policy whose safe default is in-memory only |

The broker must allow only exact registered callback and logout URIs. Public-client exchange must use an exact-origin CORS allowlist. A Studio host selects either **Direct OIDC** or **Brokered External Authentication** at deployment time; enabling both modes is a startup configuration error. Switching back to Direct OIDC is the supported rollback path.

All return targets—after local or external login, preview, and logout—must resolve to an allowlisted client-local path. User-controlled absolute or protocol-relative URLs are rejected.

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
- Host-wide administration within the connected environment plus record-ID/logical-key semantics.
- Authentication Clients and exact callback allowlists.
- PKCE broker initiation and completion.
- External Identity normalization, linking, and Elsa User resolution.
- External-only Elsa Users.
- Built-in reject and JIT Unlinked Identity Policies.
- Matcher-based existing-user resolution and static privilege-safe `defaultRoleIds` for newly created users.
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

- Host-wide environment, Connection Key, full-shadow override, and discovery hardening.
- Cluster-wide invalidation and shared broker state.
- External Identity Link administration.
- External User Matcher descriptors, static create-user roles, and Studio admission-policy authoring.
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
- **FR-011**: A Studio module MAY register a custom editor for an adapter type, but the generic renderer MUST remain sufficient to configure every adapter-declared field.

### Connection Registry and Ownership

- **FR-012**: Elsa MUST expose one effective Connection Registry composed from configuration-owned and optional database-owned connections.
- **FR-013**: Configuration-owned connections MUST be visible but read-only unless an administrator explicitly creates a full Studio Override.
- **FR-014**: Studio-owned connections and Studio Overrides MUST support create, read, update, enable, disable, archive, restore, test, and Preview Sign-in operations.
- **FR-015**: Configuration MUST provide the baseline for an immutable host-wide Connection Key. An explicit active or disabled Studio Override for that key MUST completely shadow the baseline.
- **FR-016**: Overrides MUST be whole connection documents; no field-level merge across configuration and persistence is permitted.
- **FR-017**: A disabled override MUST continue shadowing and therefore disable the logical connection. Archiving or removing the override MUST deliberately reveal the configuration baseline; restoring it MUST resume the full shadow.
- **FR-018**: Every connection MUST have a stable record ID for management/transient broker correlation and an immutable logical Connection Key for durable links and long-lived sessions.
- **FR-019**: A Connection Key MUST be unique across logical connections and sources within the currently connected Elsa server environment.
- **FR-020**: Creating a new Studio-owned connection with a configuration-owned key MUST require the explicit override operation.
- **FR-021**: Tenant-specific SSO connection administration and tenant-local key reuse are deferred beyond v1.
- **FR-022**: Connection deletion MUST logically archive the connection, preserve identity links, and emit an audit-ready notification. Audit history is retained only when an external subscriber persists those notifications.
- **FR-023**: Restoring an archived connection or override MUST preserve its Connection Key and identity links.
- **FR-024**: A genuinely different trust relationship MUST use a new Connection Key and MUST NOT inherit archived identity links.
- **FR-025**: Database-managed mutations MUST use optimistic concurrency with a revision or ETag.
- **FR-026**: Material connection changes MUST advance the revision.

### Server Environment and Tenancy

- **FR-027**: SSO connection administration MUST apply host-wide to the currently connected Elsa server environment. No persisted or editable Deployment Target/Server Environment field is part of v1.
- **FR-028**: Anonymous discovery MUST return that environment's host-wide methods only.
- **FR-029**: Provider tenant, issuer tenant, and current Studio tenant MUST NOT select a different SSO administration environment.
- **FR-030**: Connection record ID, target Elsa tenant, and material revision MUST be protected in broker state before external redirection.
- **FR-031**: External Identity Links MUST use the durable tuple `(target tenant, connectionKey, issuer namespace, stable subject)` in v1.

### Connection Settings and Secrets

- **FR-032**: Common connection fields MUST remain protocol-neutral.
- **FR-033**: Adapter-specific fields MUST NOT require new connection columns or tables.
- **FR-034**: Connection settings MUST contain Secret Bindings rather than secret values.
- **FR-035**: The module MUST resolve Secret Bindings through a pluggable resolver abstraction.
- **FR-036**: The initial implementation MUST integrate with Elsa Secrets for Managed Secrets and support replace/remove without reveal.
- **FR-037**: The initial implementation MUST provide an External Secret resolver for standard .NET configuration keys; those values and their lifecycle remain deployment-owned and read-only in Studio.
- **FR-038**: Management APIs MUST expose only secret configured state.
- **FR-039**: Studio MUST support replacement and removal only for Managed Secrets. External Secrets expose configured/resolvable state and reference metadata but no value-management actions.
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
- **FR-052**: The OIDC adapter MUST accept one exact absolute HTTPS `discoveryUrl`.
- **FR-053**: Elsa MUST derive the provider callback URI from deployment-owned external base-address configuration, the fixed adapter callback route, and immutable Connection Key so ownership changes do not alter it; Studio MUST NOT edit it.
- **FR-054**: Studio MUST expose discovery-derived issuer, authorization/token endpoints, and signing-key material as Advanced overrides when deployment policy permits and the caller holds the unsafe-provider-trust permission. Save MUST require explicit confirmation, persistent warning, and a redacted security notification.
- **FR-055**: The upstream OIDC client MUST be confidential and use authorization-code flow with S256 PKCE.
- **FR-056**: Connection administrators MUST NOT be able to weaken state, nonce, correlation, signature validation, audience, lifetime, deployment-derived callback, confidential-client, S256 PKCE, or secret-redaction invariants. Advanced values change trusted inputs, not whether validation runs.
- **FR-057**: The adapter MUST support `client_secret_basic` and `client_secret_post`, validate the full OIDC response, and declare upstream-logout capability.
- **FR-058**: Provider-specific OAuth and SAML adapters MUST be addable later without changing the connection envelope or broker completion protocol.

### Broker and Authentication Clients

- **FR-059**: Elsa Server MUST own provider redirection, callback processing, account resolution, permission resolution, and Elsa credential issuance.
- **FR-060**: Elsa MUST NOT return provider tokens or Elsa tokens in redirect URLs.
- **FR-061**: External completion MUST use a short-lived, single-use Elsa authorization code.
- **FR-062**: The completion code MUST be bound to an Authentication Client, exact callback URI, target tenant, and PKCE challenge.
- **FR-063**: Authentication Clients MUST be distinct from Elsa API Applications and MUST grant no Elsa permissions.
- **FR-064**: V1 Authentication Clients MUST be deployment-configured rather than Studio-managed.
- **FR-065**: Broker state MUST include the connection record ID and material revision.
- **FR-066**: Callback processing MUST reject a flow when the connection was disabled, archived, or materially revised after initiation.
- **FR-067**: Broker state, PKCE material, correlation state, and completion codes MUST work when initiation and completion occur on different Elsa nodes.
- **FR-068**: Material revision MUST cover adapter settings, Secret Binding identity/generation, Unlinked Identity Policy, matcher-policy settings, static `defaultRoleIds`, and override lifecycle. Display name, icon, and display order MAY use a presentation-only revision.
- **FR-069**: Initiation, callback, code exchange, and external-session refresh MUST verify authoritative enabled, archive, and effective material-revision state. Cache invalidation MAY improve freshness but MUST NOT be the security boundary.

### Elsa Users and Identity Links

- **FR-070**: Successful external authentication MUST resolve to an Elsa User before Elsa credentials are issued.
- **FR-071**: External Identity Links MUST be separate from Elsa Users.
- **FR-072**: One Elsa User MAY have multiple External Identity Links.
- **FR-073**: An Elsa User MAY exist without Local Credentials.
- **FR-074**: JIT provisioning MUST NOT generate placeholder passwords.
- **FR-075**: Elsa's User persistence model MUST be migrated so Local Credentials are absent or separate rather than represented by placeholder password hashes.
- **FR-076**: Local login for a credential-less user MUST fail with the same public result as other invalid credentials.
- **FR-077**: JIT provisioning MUST create a globally unique Elsa user name under the current identity-store contract, retry a detected name collision, and compensate the User created by a losing or failed link writer. Mutable provider profile attributes MUST NOT become identity keys. A future tenant-scoped user-name migration is outside this feature unless separately specified.
- **FR-078**: External Identity Links MUST be resolved by target tenant, immutable Connection Key, validated issuer namespace, and provider-stable subject.
- **FR-079**: Built-in behavior MUST NOT link by email or user name.
- **FR-080**: A custom Unlinked Identity Policy MAY deliberately implement deployment-specific linking behavior.
- **FR-081**: V1 MUST provide built-in reject and just-in-time creation policies.
- **FR-082**: Custom Unlinked Identity Policies MUST be deployable trusted modules with versioned settings descriptors.
- **FR-083**: Each connection MUST select its Unlinked Identity Policy; deployment configuration MUST define the default and allowed policy types.
- **FR-084**: Studio MUST show the effective per-connection policy and permit changes only when deployment policy and caller permissions allow.
- **FR-084A**: V1 MUST provide a generic matcher-based Unlinked Identity Policy that selects exactly one installed `IExternalUserMatcher` and declares a no-match action of `Reject` or `CreateUser`.
- **FR-084B**: A matcher MUST receive only its declared required normalized claims, those claims MUST remain ephemeral, and a single match MAY propose an existing Elsa User. No match follows the configured fallback; ambiguous results or errors reject.
- **FR-084C**: V1 MUST NOT ship an Elsa first-party verified-email matcher. Email/name matching remains unavailable unless a trusted deployment extension explicitly provides it.
- **FR-085**: V1 MUST support administrator-managed pre-linking and unlinking.
- **FR-086**: V1 MUST NOT support end-user self-service linking.
- **FR-087**: Complete external claim sets MUST NOT be persisted by default.

### User Matching, Role Provisioning, and Permission Resolution

- **FR-088**: Elsa MUST remain authoritative for the `permissions` claims placed in Elsa-issued credentials and MUST derive them through Elsa Roles.
- **FR-089**: Each connection MAY define static `defaultRoleIds` used only when `CreateUser` creates a new Elsa User, including the matcher policy's create-user no-match fallback.
- **FR-090**: External User Matchers MUST NOT select, derive, or mutate roles or permissions.
- **FR-091**: Saving `defaultRoleIds` MUST authorize the actor to assign every selected Role using Elsa's role-delegation rules.
- **FR-092**: JIT provisioning MUST assign authorized static default roles in the same User-store write as credential-less User creation and MUST NOT return success until the unique external identity link is durable.
- **FR-093**: Matching an existing user MUST NOT change that user's roles.
- **FR-094**: Existing linked users MUST retain their Elsa-managed role assignments; ordinary sign-in MUST NOT mutate their roles.
- **FR-095**: V1 Studio MUST NOT expose claim-to-permission, group-to-permission, wildcard, pass-through, or claim-to-role mapping UI.
- **FR-096**: External User Matcher types MUST be trusted deployed extensions with stable IDs, versioned settings, descriptors, declared required claims, and deployment allowlists.
- **FR-097**: Missing/deleted default roles or unavailable matcher extensions MUST produce validation errors or warnings and MUST NOT broaden access.
- **FR-097A**: Deleting an Elsa Role MUST inspect every database- and configuration-owned `defaultRoleIds` reference in CreateUser and matcher no-match CreateUser policies, including disabled, archived, shadowed, and currently ineffective definitions, and MUST block ordinary deletion while any reference remains.
- **FR-097B**: A configuration-owned blocker MUST identify its sanitized configuration path and policy branch. Elsa MUST NOT rewrite deployment configuration automatically.
- **FR-097C**: The administration API MUST provide an authorized, prevalidated command that removes the Role from every editable database-owned JIT policy and deletes the Role only after no reference remains. The actor MUST be authorized to delete the Role and update every affected connection/policy.
- **FR-097D**: The remediation MUST be atomic when the Role and connection stores can share a transaction. Otherwise it MUST use the documented best-effort protocol: prevalidate the complete dependency set and revisions, remove editable references before attempting Role deletion, never delete the Role after an incomplete removal, return structured partial-progress diagnostics, and support safe idempotent retry.
- **FR-097E**: Preflight and remediation MUST warn when removal leaves a CreateUser path with no default Role and MUST require explicit confirmation. A changed dependency set or stale connection revision MUST prevent Role deletion.
- **FR-097F**: V1 MUST expose this backend guard/remediation contract without adding a Role-management or Role-deletion page to External Authentication Settings. A future Elsa Roles UI MAY consume the contract.
- **FR-098**: Role expansion into permission strings MUST remain the responsibility of Elsa Identity and installed modules.
- **FR-099**: The normalized identity used during linking/JIT MAY retain only the explicitly allowed redacted provenance; matcher-required claims and complete external claims MUST NOT be persisted.
- **FR-100**: Elsa token refreshes MUST re-evaluate the user's current Elsa Role assignments.
- **FR-101**: External refresh credentials MUST reference an External Authentication Session and MUST check its connection key, maximum age, and revocation state; existing local refresh credentials remain compatible.
- **FR-102**: A configurable maximum external session age MUST require fresh provider authentication.
- **FR-103**: Upstream tokens MUST be retained only when required for the configured Elsa-initiated upstream logout, protected server-side, and no longer than the external session.
- **FR-104**: Provider access and refresh tokens MUST otherwise be discarded after callback processing and optional user-info retrieval.

### Login Discovery and Studio

- **FR-105**: Elsa MUST expose anonymous host-wide Login Method discovery.
- **FR-106**: Login Methods MUST unify local Elsa credentials and external connections without modeling local login as an Identity Provider Connection.
- **FR-107**: Anonymous discovery MUST return only method identifier/key, local-or-external kind, display name, server-hosted icon identifier, display order, preferred state, and Elsa-owned initiation URL.
- **FR-108**: Anonymous discovery MUST NOT expose adapter settings, authority, client ID, tenant internals, health details, or remote icon URLs.
- **FR-109**: Studio MUST show a chooser by default.
- **FR-110**: Administrators MAY select one enabled connection as preferred.
- **FR-111**: Preference MUST affect deterministic ordering and emphasis only; Studio MUST NOT automatically redirect past the chooser.
- **FR-112**: An unavailable preferred method MUST leave the chooser usable and show a safe status.
- **FR-113**: The initial Studio module MUST support Blazor Server and Blazor WebAssembly.
- **FR-114**: Blazor Server MUST use a confidential Authentication Client; the Studio host MUST perform exchange, keep Elsa refresh credentials server-side, and establish a secure HTTP-only browser session.
- **FR-115**: Blazor WebAssembly MUST use a public Authentication Client with no client secret, mandatory PKCE, exact-origin CORS, and an explicit token-storage policy that defaults to in-memory storage.
- **FR-116**: Studio MUST distinguish the connection's Upstream Client Registration from the deployment-owned Elsa Authentication Client in labels, help text, validation, and prerequisites.
- **FR-117**: Studio MUST provide a dedicated External Identity Links page with an Elsa User picker and a reusable user-link panel under Security.
- **FR-118**: Link administration MUST include a tenant-scoped, permission-guarded, paginated Elsa User lookup returning only minimal selection data.
- **FR-119**: Full user and role CRUD UI MUST remain outside this feature.
- **FR-119A**: `Elsa.Studio.Authentication.UI` MUST own the generic login/logout shell and contribution contracts; the External Authentication module MUST contribute behavior without owning the shell.
- **FR-119B**: Settings MUST be a Studio composition/navigation surface only. SSO connection administration belongs at one-level **Settings → SSO** (`/settings/sso-connections`); External Identity Links and External Authentication Sessions remain separate Security pages.

### Logout and Session Control

- **FR-120**: Normal logout MUST end the Elsa session.
- **FR-121**: Connections whose adapters support Upstream Logout MUST expose `Disabled`, `UserChoice`, and `Always` modes.
- **FR-122**: Upstream Logout MUST default to `Disabled`.
- **FR-123**: Disabling or archiving a connection MUST reject new sign-ins, in-flight callbacks, and further Elsa token refreshes associated with it.
- **FR-124**: Existing short-lived access tokens MUST remain valid until expiry unless an explicit supported revocation action invalidates them.
- **FR-125**: Session revocation MUST be a separate audited action when server-side revocation support is enabled.
- **FR-125A**: V1 MUST support only Elsa-initiated sign-in and logout. Unsolicited IdP-initiated login and front-channel/back-channel provider-initiated logout are out of scope.

### Administration, Recovery, and Audit

- **FR-126**: Connection operations MUST use dedicated Elsa permissions for read, create, update, archive/restore, test, policy management, unsafe security overrides, identity-link management, and session revocation.
- **FR-127**: Configuration-owned connections MUST remain immutable through runtime APIs regardless of caller permissions.
- **FR-128**: Elsa MUST support a configurable final-login-path lockout guard.
- **FR-129**: When the guard is active, disabling the final valid sign-in path MUST require a deployment-owned Break-glass Authentication method or an explicitly privileged confirmed override.
- **FR-130**: Break-glass Authentication MUST NOT appear in normal Login Method discovery.
- **FR-131**: The module MUST publish typed, immutable, redacted security notifications through `INotificationSender`.
- **FR-132**: Notifications MUST cover connection and policy changes, secret replacement/removal, enable/disable/archive/restore, tests, previews, link changes, session revocation, and sign-in outcomes.
- **FR-133**: Notifications SHOULD contain actor, Connection Key, Elsa User ID when known, timestamp, outcome, correlation ID, and a redacted change summary.
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
- **FR-142**: A Studio host MUST select one active authentication mode. Direct OIDC and Brokered External Authentication may coexist as installed modules but MUST NOT both own login routes in one host.
- **FR-143**: Studio MUST show host-wide Login Methods only in v1 and MUST NOT offer an anonymous tenant picker.
- **FR-144**: Adapter outbound HTTP used for discovery, testing, preview, and callbacks MUST apply deployment egress policy, HTTPS-by-default, bounded time and response size, controlled redirects, DNS and resolved-address checks, and redacted exception handling.
- **FR-145**: Deployment policy MUST be able to deny private, loopback, link-local, reserved, or unapproved provider destinations and to route adapter traffic through an approved proxy.
- **FR-146**: Preview MUST use separate short-lived, one-time state and result records bound to administrator, connection record ID, draft revision, and preview callback.
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
- **FR-157**: Direct OIDC MAY be marked deprecated only after broker parity and migration tooling are available; it MUST remain supported throughout Elsa 3.x.
- **FR-158**: Removal of Direct OIDC MUST require a future major release, advance notice, and a tested rollback/migration guide.

## Conceptual Data Model

| Concept | Purpose | Important characteristics |
| --- | --- | --- |
| Identity Provider Connection | Elsa's trust relationship with an external provider | Stable management record ID, immutable host-wide Connection Key, source/override provenance, adapter type, presentation, preferred/enabled/archive state, revision, versioned settings, secret bindings, policy and static create-user roles |
| Adapter Descriptor | Describes an installed adapter | Type, version, fields, validation, UI hints, secret fields, capabilities, optional custom editor key |
| External Identity Link | Associates external identity with Elsa authorization | Target tenant, Connection Key, issuer, subject, Elsa User ID, timestamps |
| Elsa User | Owns Elsa authorization | Tenant context, optional Local Credential, roles and other Elsa-specific data |
| Unlinked Identity Policy Selection | Decides what to do with an unknown identity | Policy type, settings version, settings, inherited or connection override |
| External User Matcher Selection | Proposes an existing user for an unlinked identity | One matcher type, versioned settings, declared required ephemeral claims, no-match fallback |
| Authentication Client | Safe broker return target | Client ID, exact callback URIs, client type, PKCE requirement, optional logout callbacks |
| External Authentication Session | Bounded Elsa session created from external authentication | User, Connection Key, minimal normalized identity provenance, maximum age, refresh/revocation state, optional protected logout artifact |
| Secret Binding | Resolves sensitive adapter values | Managed or External ownership, resolver type, non-secret reference, configured/resolvable state |

## Management Capabilities

The specification should define exact routes and schemas for:

- Connection list, detail, create, update, enable, disable, archive, and restore.
- Explicit full-shadow override creation, reporting, archive/reveal, restore, and removal.
- Installed Adapter Descriptor discovery.
- Effective policy, Role option, and External User Matcher descriptor discovery.
- Secret configured state, replacement, and removal.
- Structural validation and non-interactive testing.
- Interactive Preview Sign-in initiation and redacted result retrieval.
- Identity Link list, create, and unlink, plus bounded tenant-scoped user lookup.
- Session list/revocation where enabled.
- Anonymous Login Method discovery.
- Local-login initiation and normalized completion.
- Broker initiation, provider callback, completion-code exchange, logout, and optional Upstream Logout.

## Studio Information Architecture

```text
Settings
└── SSO
    ├── Connections
    ├── Create/Edit or Override
    ├── Test
    └── Preview Sign-in

Security
├── External Identity Links
└── External Authentication Sessions
```

Settings is a Studio UI composition and navigation surface only. It does not introduce server-side Settings entities or generic Settings persistence. `Elsa.Studio.Authentication.UI` owns the clean login/logout shell and accepts contributions from local, external, and future authentication modules.

The connection list should support filtering by source, adapter type, enabled state, validity, override/shadow state, and archived state. The currently connected server environment supplies the host-wide context; no target field is rendered.

Configuration-owned connections are inspect-only until the administrator explicitly chooses **Create Studio Override**. Every page and API independently enforces authorization; menu visibility is only an affordance. The connection editor labels provider-issued fields as **Upstream Client Registration**, shows the deployment-derived callback as read-only, and displays the eligible deployment-owned **Elsa Authentication Client** only as a redacted prerequisite.

## Security and Privacy Requirements

- Use exact callback URI matching.
- Protect broker state against tampering and replay.
- Use PKCE for Elsa's completion-code handoff.
- Keep provider-facing protocol security under adapter control while preserving Broker Security Invariants.
- Never put tokens or secrets in URLs.
- Reject open redirects by allowing only registered callbacks and client-local return paths.
- Never reveal stored secrets.
- Never auto-link by mutable profile attributes.
- Never map external claims directly to Elsa permissions in v1.
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

### C. Explicit Studio Override

Given a configuration connection, when an authorized administrator creates a Studio Override for its immutable key, then the complete override shadows configuration without field merging. Disabling the override keeps the logical connection disabled; archiving it reveals configuration; restoring it resumes the shadow.

### D. JIT external-only user

Given a successful external identity with no link and an effective JIT policy, when the broker completes sign-in, then Elsa creates an Elsa User without local password material, assigns authorized default/matcher roles with that User write, publishes one durable link, compensates a failed publication, and issues Elsa credentials from Elsa role permissions only after both records exist.

### E. Pre-provisioned-only user

Given an effective reject-unlinked policy, when an unlinked identity authenticates, then Elsa returns a safe denial. After an authorized administrator pre-links the identity, the same external sign-in succeeds.

### F. Matcher-based admission and static create-user roles

Given the matcher-based policy, when its one `IExternalUserMatcher` returns one user, Elsa links that user without changing roles. No match rejects or creates a new user according to configuration; create-user assigns only authorized static `defaultRoleIds`. Ambiguous/error results reject, and matcher claims are not retained.

### G. Host-wide connected environment

Given Studio is connected to an Elsa server environment, when SSO connections are administered or discovered, then they apply host-wide to that environment without a target entity or editable environment field.

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

### N. Preferred method

Given a preferred enabled connection, when Studio renders login, then that method is ordered and emphasized while the complete chooser remains visible and no automatic redirect occurs.

### O. Administrator lockout

Given final-login-path protection is active and no verified recovery path or privileged override is present, when an administrator attempts to disable the last valid method, then Elsa rejects the operation.

### P. Compatibility

Given an existing Studio direct OIDC deployment, when the new module is introduced but not selected, then existing authentication continues unchanged.

### Q. Studio hosting profiles

Given a Blazor Server client, when brokered login completes, then the server exchanges the code and the browser receives only an HTTP-only Studio session. Given a Blazor WebAssembly public client, then no client secret is accepted, PKCE and exact-origin CORS are enforced, and no credential appears in a URL.

### R. Redirect safety

Given an attacker supplies an absolute or protocol-relative return target to login, chooser recovery, preview, or logout, when Elsa or Studio validates it, then the target is rejected rather than followed.

### S. Role delegation boundary

Given a connection administrator may not assign an Elsa Role, when they add it directly or make it reachable through a matcher, then Elsa rejects the change. No claim-permission mapping controls are present.

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

### Y. Role deletion dependency guard

Given an Elsa Role is referenced by any CreateUser or matcher no-match CreateUser `defaultRoleIds`, when ordinary Role deletion is requested, then deletion is blocked and all safe database/configuration reference diagnostics are returned. Configuration references include their sanitized configuration paths and remain untouched. When only editable database references remain and an authorized administrator confirms all empty-default-role and best-effort warnings, remediation removes the Role from every referenced policy and deletes the Role only after revalidation proves no dependency remains; any incomplete best-effort run leaves the Role intact and reports safe retry state.

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
- Role-lifecycle tests prove ordinary deletion is blocked by every persisted or configured JIT-policy reference, configuration paths are actionable but never mutated, and remediation cannot delete the Role after incomplete reference removal.
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
