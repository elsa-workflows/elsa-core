# External Integrations

## Integration Inventory

| System | Type | Purpose | Auth model | Criticality | Evidence |
|---|---|---|---|---|---|
| External identity providers | OIDC adapter | Authenticate upstream users | Authorization code/OIDC callback | High | `Elsa.ExternalAuthentication.OpenIdConnect` |
| Relational databases | EF Core | Durable external-auth state and links | Deployment connection string | High | `Elsa.ExternalAuthentication.Persistence.EFCore.*` |
| Elsa Studio/client | REST | Discover login methods and manage links | Elsa permissions/tenant context | High | `Endpoints/IdentityLinks`, `Elsa.Api.Client` |

## Data Stores

| Store | Role | Access layer | Key risk | Evidence |
|---|---|---|---|---|
| In-memory dictionaries | Single-node development/test state | In-memory provisioners/stores | Not shared across nodes | `Stores/InMemory`, `InMemoryExternalIdentityProvisioner.cs` |
| EF Core relational store | Durable links, sessions, grants, transactions | EF Core store implementations | Cross-store completion is not one transaction | `Elsa.ExternalAuthentication.Persistence.EFCore` |

## Secrets and Credentials Handling

- Connection secrets use secret bindings and resolver abstractions.
- External subjects are HMAC-hashed before persistence.
- Safe identity-link API documents omit both raw subjects and subject hashes.

## Reliability and Failure Behavior

- Callback state is one-time and bounded by expiry.
- Failed/cancelled adapter callbacks do not reach the successful-sign-in tracking boundary.
- Durable link activity uses compare-and-set semantics to retain the newest concurrent timestamp.

## Observability for Integrations

- Broker outcomes publish bounded security notifications.
- Connection tests record health observations without mutating sign-in activity.
- `[TODO]` Repository-wide tracing/metrics inventory was outside this focused investigation.

## Evidence

- `src/modules/Elsa.ExternalAuthentication.OpenIdConnect/Services/OpenIdConnectExternalAuthenticationAdapter.cs`
- `src/modules/Elsa.ExternalAuthentication/Services/HmacExternalAuthenticationHandleHasher.cs`
- `src/modules/Elsa.ExternalAuthentication/Services/ConnectionTestService.cs`
- `src/modules/Elsa.ExternalAuthentication/Notifications/ExternalAuthenticationSecurityNotifications.cs`

