# Studio Contract: External Authentication

## Packages

```text
src/modules/
├── Elsa.Studio.Settings/
├── Elsa.Studio.Authentication.UI/
├── Elsa.Studio.ExternalAuthentication/
├── Elsa.Studio.ExternalAuthentication.BlazorServer/
└── Elsa.Studio.ExternalAuthentication.BlazorWasm/
```

### Shared Module

`Elsa.Studio.Settings` owns UI composition/navigation contracts only. It does not define a server Settings entity, generic Settings persistence, or authorization semantics.

`Elsa.Studio.Authentication.UI` owns:

- The generic `/login` and `/logout` shell.
- Login Method chooser layout, accessibility, safe return paths, and error presentation.
- Authentication UI contribution contracts for local, brokered external, and future methods.
- Generic authentication navigation without provider-specific business logic.

`Elsa.Studio.ExternalAuthentication` contributes:

- Refit clients and API DTOs.
- Broker-backed Login Method provider and local/external contributions to the Authentication.UI shell.
- PKCE request model and external-authentication-specific state.
- Identity Provider Connection list/editor/test/preview.
- External Identity Link page and bounded user picker.
- Descriptor-driven field renderer and optional custom-editor registry.
- Settings navigation contribution for Connections and Security contributions for Links/Sessions.
- Common broker token/provider abstractions.

Registration:

```csharp
services.AddExternalAuthenticationModule(backendApiConfig);
```

The module registers management only when the remote feature is available. It never replaces the Authentication.UI shell; the selected brokered host package activates its contributions.

### UI Contribution Contracts

```csharp
public interface ILoginMethodCatalog;
public interface ILoginMethodComponentProvider;
public interface ILoginMethodIconProvider;

public interface ISettingsNavigationContributor
{
    ValueTask ContributeAsync(
        SettingsNavigationBuilder builder,
        CancellationToken cancellationToken = default);
}

public interface ISecurityMenuContributor
{
    ValueTask ContributeAsync(
        SecurityMenuBuilder builder,
        CancellationToken cancellationToken = default);
}
```

`ILoginMethodCatalog` supplies ordered method metadata, `ILoginMethodComponentProvider` resolves method-specific UI, and `ILoginMethodIconProvider` resolves trusted local icons. Settings/Security contributors add navigation metadata only. None replace the shell, create duplicate parents, bypass route/API authorization, or persist arbitrary Settings data.

### Server Host Package

`Elsa.Studio.ExternalAuthentication.BlazorServer` owns:

- `ElsaStudio.ExternalAuthentication.Cookie` scheme.
- Protected server-side broker transaction state.
- `/authentication/external/callback`.
- `/authentication/external/logout-callback`.
- Back-channel code exchange and refresh.
- Server token provider for API clients and SignalR.
- Unauthorized component that redirects to `/login`.
- Cookie/session expiration and logout.

Registration:

```csharp
services.AddExternalAuthenticationBroker(options =>
    configuration.GetSection("Authentication:ExternalAuthentication").Bind(options));
```

Cookie requirements:

- Name: `ElsaStudio.ExternalAuthentication`.
- `HttpOnly = true`.
- `Secure = Always`.
- `SameSite = Lax`.
- No Elsa or provider token readable by browser code.
- Antiforgery required on state-changing BFF endpoints.
- Session lifetime does not exceed the external session.

The callback performs client-state and PKCE transaction validation before sending the Elsa completion code back-channel.

### WebAssembly Host Package

`Elsa.Studio.ExternalAuthentication.BlazorWasm` owns:

- Browser-crypto PKCE verifier/challenge generation.
- Pre-exchange transaction state in `sessionStorage`, deleted after callback.
- `/authentication/external/callback`.
- `/authentication/external/logout-callback`.
- Direct public-client code exchange and refresh.
- In-memory token provider by default.
- API/SignalR authorization integration.
- Unauthorized component that redirects to `/login`.

Registration uses the same method name with host-specific overload/assembly:

```csharp
services.AddExternalAuthenticationBroker(options =>
    configuration.GetSection("Authentication:ExternalAuthentication").Bind(options));
```

No client secret is accepted. Persistent post-exchange browser token storage is an explicit deployment opt-in and displays a startup/security warning.

## Authentication Provider Selection

The host reads `Authentication:Provider`:

| Value | Existing/new behavior | Broker chooser |
| --- | --- | --- |
| `ElsaIdentity` | Existing Elsa username/password Studio authentication | Off |
| `OpenIdConnect` | Existing direct single-provider OpenID Connect | Off |
| `ElsaLogin` | Existing legacy login module | Off |
| `ExternalAuthentication` | New Elsa broker client | On |

Server retains its existing default (`ElsaIdentity`); WebAssembly retains its existing default (`OpenIdConnect`). Broker mode is explicit opt-in.

Startup fails when:

- `Authentication:Provider = ExternalAuthentication` but broker client ID/callback is missing.
- Broker mode attempts to register a public client on Server or confidential secret on WebAssembly.
- Direct OpenID Connect handlers are also enabled in broker mode.
- Callback, logout callback, or browser origin does not match the server-side Authentication Client registration.

Direct settings remain untouched for rollback.

## Login Chooser Contribution

Route: `/login`

Flow:

1. Resolve the validated client-local `returnPath`; invalid values become `/`.
2. Fetch anonymous Login Methods with `ILoginMethodsApi`.
3. Render all available local and external methods in deterministic order.
4. Order and emphasize the preferred method without automatic redirect.
5. Generate PKCE before initiating either local or external broker flow.
6. Preserve only opaque client transaction state and local return path.

Presentation:

- Reuse `BasicLayout`, branding, and localization.
- Render a visible method name even if an icon fails.
- Resolve only trusted icon IDs through a local asset registry.
- Full keyboard operation, visible focus, accessible names, and status/error announcements.
- Do not render provider HTML, remote images, authority, or client identifiers.

## Shared Refit Clients

```csharp
public interface ILoginMethodsApi;
public interface IExternalAuthenticationBrokerApi;
public interface IIdentityProviderConnectionsApi;
public interface IExternalIdentityLinksApi;
public interface IExternalAuthenticationSessionsApi;
```

- `ILoginMethodsApi` uses the anonymous backend API provider.
- Broker exchange uses a dedicated client without an existing bearer handler.
- Management, link, and session clients use the authenticated backend API provider.
- DTOs mirror [rest-api.md](rest-api.md) and do not define alternate Studio-only payloads.

## Management Information Architecture

```text
Settings
└── SSO

Security
├── External Identity Links
└── External Authentication Sessions
```

Routes:

- `/settings/sso-connections`
- `/settings/sso-connections/new`
- `/settings/sso-connections/{connectionId}`
- `/security/external-identity-links`
- `/security/external-authentication-sessions`

Legacy connection-route aliases may remain for bookmarked links, but generated navigation and documentation use `/settings/sso-connections`.

`Elsa.Studio.Settings` owns Settings navigation and `ISettingsNavigationContributor`; External Authentication contributes one-level SSO. `Elsa.Studio.Security` owns Security and `ISecurityMenuContributor`; External Authentication contributes Links and capability-gated Sessions. Neither menu grants authorization.

## Connection List

The list is server-paged and filters by:

- Search.
- Source.
- Adapter type.
- Enabled intent.
- Validity.
- Override/shadow state.
- Archive state.

Columns show display name/key, record ID, source/override relationship, adapter, enabled intent, validity, latest on-demand test with timestamp/revision/staleness, preferred state, and revision.

Behavior:

- Configuration-owned rows are inspect-only and offer **Create Studio Override** when authorized.
- Overrides visibly identify the configuration baseline and state that no fields are merged.
- Disabled overrides keep shadowing; archived overrides reveal configuration; restore resumes shadowing.
- Archived rows permit restore only when authorized.
- Caller permissions control action visibility, but every API remains authoritative.

## Connection Editor

Sections:

1. **Identity**: immutable Connection Key, display name, icon, order, preferred. The connected server environment is implicit and no target field is shown.
2. **Provider adapter**: installed adapter and descriptor-driven fields.
3. **Upstream Client Registration**: exact `discoveryUrl`, confidential client ID, `client_secret_basic`/`client_secret_post`, Managed/External Secret Binding, read-only deployment-derived callback, and scopes.
4. **Advanced Provider Trust**: permission/deployment-gated overrides for discovery-derived issuer, authorization/token endpoints, and signing keys. The section is collapsed by default, labels values unsafe, requires confirmation on save, and keeps a persistent warning while any override is active.
5. **Claim projection**: allowed/redacted claims and size limits.
6. **Admission and create-user roles**: per-connection policy; one External User Matcher and Reject/CreateUser no-match action for matcher policy; static `defaultRoleIds` only for CreateUser; role-assignment warnings.
7. **Authorization explanation**: matchers propose users, never roles; Elsa Roles produce permissions and no claim-role/permission mapping controls are rendered in v1.
8. **Logout**: capability-driven mode.
9. **Status**: enabled intent, validity, source/shadowing, latest test, revision.

Advanced trust controls never expose switches for callback derivation, confidential-client mode, S256 PKCE, state/correlation/nonce, signature validation, audience/lifetime validation, one-time codes, or secret redaction.

Secret fields show only configured/resolvable state. Managed Secrets have replace/remove actions; External Secrets show deployment-owned reference/state and no value-lifecycle action. The editor never binds a returned secret value.

Save uses `If-Match`. On `412`, Studio preserves unsaved values, loads the current safe model, and offers reload or manual reapply—never silent overwrite.

## Test and Preview

### Test

- Requires test permission.
- Shows progress, then redacted category, summary, warnings, duration, correlation ID, tested revision, and timestamp.
- Never labels the result continuous health.
- Immediately marks a result stale after a material edit.

### Preview Sign-in

- Requires preview permission and explicit confirmation that no normal session/user/link will be created.
- Opens the Elsa-owned preview navigation URL.
- On return, reads the result once from the initiating active administrator session.
- Displays masked identity, allowlisted preview claims, proposed match/no-match policy decision, safe proposed user/link action, static create-user roles when applicable, and warnings. Matcher-required claims are not retained in the result.
- If the administrator session is gone, display an expired result and do not retry/read it through anonymous state.

## External Identity Links

The dedicated page:

- Uses the tenant-scoped paginated user option API.
- Filters links by user and connection.
- Displays connection, issuer, masked subject hint, created time, and last sign-in.
- Creates prelinks from explicit issuer and subject input.
- Requires confirmation before unlink.
- Never displays provider tokens, raw stored subject, full claims, credential fields, roles, or permission lists.

The user-link panel is a reusable component for a future user detail page.

## Session Administration

When the server advertises session administration:

- List safe session metadata by user/connection/status.
- Revoke with confirmation and reason.
- Never display access/refresh tokens, hashes, raw subject, or claim snapshot.

Hide the section when the remote capability is absent.

## UI Authorization Matrix

| Capability | UI behavior |
| --- | --- |
| Read only | Show list/detail; no mutation buttons |
| Create | Show New |
| Update | Edit database rows; enable/disable; secret binding actions |
| Archive | Archive/restore |
| Test | Test button |
| Preview | Preview button |
| Policy manage | Policy/grant editors |
| Unsafe provider trust | Unsafe controls with warning/confirmation |
| Link manage | Link page and mutations |
| Session read/revoke | Session section and revoke |

Route authorization and API authorization are both required. Menu visibility alone grants nothing.

## Role Lifecycle UI Boundary

External Authentication contributes the backend Role-deletion dependency and remediation contract but no Role-management page or additional Settings route. A future Elsa Roles UI MAY call the deletion-impact and remediation APIs, display configuration paths and last-default-role warnings, and collect the required confirmations. Until such a UI exists, the contract remains available to authorized API clients.

## Studio Verification

- Unit tests for descriptor form mapping, safe secret state, menu contribution, authorization affordances, preferred ordering without redirect, icon fallback, return-path validation, ETag recovery, and stale test labels.
- Server integration tests for protected transaction state, callback, back-channel exchange, cookie flags, server-only tokens, refresh, logout, API/SignalR token attachment, and conflicting startup mode.
- Browser tests for WebAssembly redirect round-trip, PKCE, exact-origin CORS, no client secret, transaction cleanup, memory token loss on reload, and chooser accessibility.
- Cross-repository tests run both Studio hosts against the same fake provider and Core broker fixture.
