# Tenant selection and claims

Each configured server chooses how Studio obtains its tenant. Existing
configurations keep `fixed` mode and the existing optional `tenantId` header.
Tenant configuration is public browser configuration; it grants no backend access.

```json
{
  "servers": [{
    "name": "Operations",
    "url": "https://example.com/elsa/api",
    "tenantId": "north",
    "tenancy": {
      "mode": "selection",
      "tenants": [
        { "id": "", "name": "Default tenant" },
        { "id": "north", "name": "North" },
        { "id": "south", "name": "South" }
      ]
    }
  }]
}
```

The header picker offers configured choices only. It starts with a valid saved
selection, then a configured `tenantId` default, then the first choice. Duplicate
IDs and an empty choice list are rejected. The empty string is Elsa's explicit
default-tenant ID; it is sent as an empty `X-Tenant-Id` header. An unspecified tenant
omits that header. Those cases differ in the backend's resolver pipeline.

For account-bound tenancy, use `"tenancy": { "mode": "claims" }`. The default
claim type is `http://schemas.microsoft.com/identity/claims/tenantid`, matching Elsa
Identity. Set `claimType` for another identity provider. Claims mode ignores a saved
selection or fixed header default, displays the current tenant without a selector,
and blocks module API requests if the authenticated account lacks the configured
claim. An explicitly empty claim identifies the default tenant. Login remains
available before authentication.

`fixed` mode uses `tenantId` as supplied, including an explicit empty default ID.
`STUDIO_TENANT_ID` sets that value on the first server; it is also an initial choice
in selection mode when listed, and does not override claims mode.

## Refresh and isolation

A selected tenant is remembered per normalized API URL and authenticated subject
in browser storage. The user name is the fallback when the provider supplies no
subject identifier. Preferences for different servers and subjects are separate;
an unconfigured stored ID is ignored. Storage failure leaves the current context
unchanged and displays a recovery message.

Switching tenant or server navigates to the Studio home page and reloads the
application. This resets every first- and third-party module's stores, open editors,
dialogs, clipboard, queries and pending browser requests. Authentication and UI
preferences keep their existing persistence. A dirty workflow offers cancellation
before changing the tenant preference; accepting discards its changes without a
second browser prompt. An authenticated identity or claim-tenant change also
reloads the application. Query scopes include server, subject and tenant; replacing
a scope destroys its owned query cache and cancels its queries.

Modules receive the shared client, so API calls use the effective tenant header.
The client checks the originating account and effective tenant before credentials,
immediately before sending, and after a 401 refresh. If that scope changes or a
required tenant claim disappears, it cancels the pending operation instead of
sending its existing payload with another account or tenant. A same-scope refresh,
including an explicit default-tenant claim, preserves the operation.
`useTenant` and `TenantProvider` are public SDK exports from `/tenancy`. Modules
with independent forms can register dirty state with `useUnsavedChanges` from
`/unsaved-changes`. Hosts that confirm discarding changes can call
`approveNavigation` immediately before navigating, preserving the ordinary browser
guard for later navigations.

`reloadStudioContext` from `/navigation/reload` coordinates a single context reload.
Its preparation callback runs after marking navigation pending, so a synchronous
server-store update cannot race a second reload. The last valid authenticated
context is retained above the missing-claim error view: moving from tenant A through
a missing claim to tenant B still reloads before B's modules mount. If a native
unsaved-changes prompt cancels an automatic account-context reload, module UI remains
blocked until the reload is retried. Query-cache disposal distinguishes a true
unmount from React StrictMode's effect replay.

## Backend configuration

The backend remains responsible for tenant resolution and authorization. Selection
mode requires a backend policy that accepts the tenant header and authorizes the
account for the selected tenant. The reference Core host currently uses
`CurrentUserTenantResolver`; it resolves the account's tenant and does not honor
selection headers. Use its account-bound tenancy or configure an authorized switch
policy before enabling selection. A picker entry cannot change that policy.

Browser tests verify selected/default headers, fresh graphs for identical definition
IDs in different tenants, full module-state reload, per-server preferences,
canceling/accepting dirty-workflow navigation, claim headers and missing-claim
request blocking. Unit coverage verifies policy precedence, invalid selections,
default IDs and preference identity. These UI tests stub backend data; they do not
change or certify a deployment's tenant authorization policy.
