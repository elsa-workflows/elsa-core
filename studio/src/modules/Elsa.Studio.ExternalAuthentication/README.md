# Elsa.Studio.ExternalAuthentication

This module contributes broker login methods to the shared authentication UI and provides the administration UI for Elsa Server's External Authentication broker. It does not own `/login`. Host-specific packages provide the credential boundary:

- `Elsa.Studio.ExternalAuthentication.BlazorServer` uses a confidential Authentication Client, exchanges codes on the server, retains refresh credentials server-side, and establishes an HTTP-only browser session.
- `Elsa.Studio.ExternalAuthentication.BlazorWasm` uses a public Authentication Client with mandatory PKCE. Credentials are memory-only by default; session or durable browser storage is an explicit warned deployment option.

Brokered authentication is opt-in. Existing direct `Elsa.Studio.Authentication.OpenIdConnect` behavior remains available, although brokered external authentication is recommended for new deployments that need runtime-managed providers. A Studio host selects exactly one mode through `Authentication:Provider`; direct and brokered OpenID Connect must not be enabled together.

The broker-local login endpoint is also additive. It does not replace Elsa's existing direct `/identity/login` or refresh contracts.

See [the Studio migration guide](../../../docs/migrations/external-authentication.md) for setting mappings, rollout, and rollback.

## Routes and permissions

The shared module contributes:

- `/login` is owned by `Elsa.Studio.Authentication.UI`; this module contributes local-broker and external-provider methods.
- `/security/external-authentication/connections` for identity provider connection management.
- `/security/external-authentication/identity-links` for tenant-scoped prelink and unlink operations.
- `/security/external-authentication/sessions` for optional session list and revocation.

Menus and buttons are permission-gated usability affordances; Elsa API authorization remains authoritative. Connection test and Preview Sign-in use the current revision. Preview opens in a separate tab and returns a one-time redacted result without creating a user, link, credential, or normal session.

Connections supplied by deployment configuration remain visible but read-only. A privileged admin
may create an explicit, complete Studio override; the request carries the override flag but clears
all secret bindings so the administrator must deliberately reconfigure them after saving the
draft. Secret values are never cloned. Connection keys become immutable after creation. Provider
callback URIs are derived by Elsa from deployment-owned public-origin configuration and displayed
read-only. The override action appears only when both the actor's create permission and the
server's `canCreateOverride` deployment capability allow it.

Secret fields use a managed, write-only value editor when the server advertises an installed
managed-secret resolver. Studio sends the value only to the managed-secret replacement endpoint
and clears its local input; responses contain ownership and configured/resolvable state, never the
value. Deployment-managed resolver/reference bindings are intentionally not displayed or editable
in Studio. When no managed resolver is advertised, Studio hides the managed editor and explains
why. Required managed secrets cannot be removed while their connection remains enabled.

The preferred sign-in method is visual guidance only. Login never redirects automatically.
Permission and claim mapping DTOs remain available for contract compatibility, but this release
does not expose customer-facing mapping or permission-preview editors.

Unlinked-identity policy forms are descriptor-driven. The `match-user` policy is offered only
when the backend advertises at least one installed user matcher; Studio renders that matcher's
fields and required-claim information instead of exposing raw matcher JSON. Create-user outcomes
use role options from `/identity/roles`. The role picker requires `read:role` and becomes read-only
with a warning when roles cannot be loaded.

The deprecated `Elsa.Studio.Login` package retains its legacy `/login` page only when selected by
itself. Do not register it with the shared authentication UI or broker packages. Startup
validation rejects mixed legacy-login, Elsa Identity, direct OIDC, and broker registrations.
Migrate legacy hosts by selecting `ElsaIdentity` or `ExternalAuthentication`, removing
`AddLoginModule()`, and registering the shared authentication UI in the composition root.

## Management contract seam

The Studio module owns host-local Refit contracts under `Client/` so the independently versioned Studio packages can consume the frozen External Authentication REST shape. Generated `Elsa.Api.Client` resources expose the equivalent Core contract to other .NET clients. The Studio models intentionally mirror descriptor fields, adapter capabilities, connection operations, identity links, and session metadata.

Studio sends a managed secret value only to the dedicated write-only replacement endpoint and
never displays secret values or deployment binding references. Preview, test, and session models
contain only the redacted fields documented by the Core REST contract.
