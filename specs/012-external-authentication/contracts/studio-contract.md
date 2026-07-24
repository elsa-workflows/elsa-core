# Studio Contract: External Authentication

## Packages

```text
src/modules/
├── Elsa.Studio.ExternalAuthentication/
├── Elsa.Studio.ExternalAuthentication.BlazorServer/
└── Elsa.Studio.ExternalAuthentication.BlazorWasm/
```

### Shared Module

`Elsa.Studio.ExternalAuthentication` owns:

- Refit clients and API DTOs.
- Login Method chooser and local credential form.
- PKCE request model and safe local return-path validation.
- Identity Provider Connection list/editor/test/preview.
- External Identity Link page and bounded user picker.
- Descriptor-driven field renderer and optional custom-editor registry.
- Security menu contributions.
- Common broker token/provider abstractions.

Registration:

```csharp
services.AddExternalAuthenticationModule(backendApiConfig);
```

The module registers management only when the remote feature is available. The chooser is activated only by the selected brokered authentication host package.

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

## Login Chooser

Route: `/login`

Flow:

1. Resolve the validated client-local `returnPath`; invalid values become `/`.
2. Fetch anonymous Login Methods with `ILoginMethodsApi`.
3. If automatic redirect is eligible and no escape flag is present, navigate once to its Elsa initiation URL.
4. If redirect fails or returns an error, mark that method attempted and show the chooser to prevent loops.
5. Render local credentials and external methods in deterministic order.
6. Generate PKCE before initiating either local or external broker flow.
7. Preserve only opaque client transaction state and local return path.

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
Security
├── Identity Provider Connections
└── External Identity Links
```

Routes:

- `/security/identity-provider-connections`
- `/security/identity-provider-connections/new`
- `/security/identity-provider-connections/{connectionId}`
- `/security/external-identity-links`

`Elsa.Studio.Security` owns the single Security parent. Add `ISecurityMenuContributor`; External Authentication contributes its two children. This prevents duplicate parent menus as more security modules are added.

## Connection List

The list is server-paged and filters by:

- Search.
- Source.
- Connection Scope.
- Adapter type.
- Enabled intent.
- Validity.
- Shadow/conflict state.
- Archive state.

Columns show display name/key, source, scope, adapter, enabled intent, validity, latest on-demand test with timestamp/revision/staleness, default state, and revision.

Behavior:

- Configuration-owned rows are inspect-only.
- Shadowed rows explain the winning configuration source.
- Archived rows permit restore only when authorized.
- Caller permissions control action visibility, but every API remains authoritative.

## Connection Editor

Sections:

1. **Identity and scope**: key, host/default/tenant scope, display name, icon, order, default.
2. **Provider adapter**: installed adapter and descriptor-driven fields.
3. **Upstream Client Registration**: provider client ID and Secret Binding fields, explicitly distinguished from Elsa Authentication Client prerequisites.
4. **Claim projection**: allowed/redacted claims and size limits.
5. **Admission policy**: effective default, optional allowed override, settings.
6. **Permission grants**: ordered sources, descriptors/warnings, provenance preview.
7. **Logout**: capability-driven mode.
8. **Status**: enabled intent, validity, source/shadowing, latest test, revision.

Secret fields show only configured/resolvable state with replace/remove actions. The editor never binds a returned secret value.

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
- Displays masked identity, allowlisted claims, proposed policy decision, proposed user/link action, projected permission grants with provenance, and warnings.
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

## Studio Verification

- Unit tests for descriptor form mapping, safe secret state, menu contribution, authorization affordances, ordering, icon fallback, return-path validation, automatic redirect loop prevention, ETag recovery, and stale test labels.
- Server integration tests for protected transaction state, callback, back-channel exchange, cookie flags, server-only tokens, refresh, logout, API/SignalR token attachment, and conflicting startup mode.
- Browser tests for WebAssembly redirect round-trip, PKCE, exact-origin CORS, no client secret, transaction cleanup, memory token loss on reload, and chooser accessibility.
- Cross-repository tests run both Studio hosts against the same fake provider and Core broker fixture.
