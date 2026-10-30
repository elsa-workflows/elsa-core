# Migrate Studio from Direct OpenID Connect to Brokered Authentication

Elsa Studio supports the existing direct OpenID Connect integration and the Elsa Server broker as separate deployment modes. Select one mode per host with `Authentication:Provider`:

- `OpenIdConnect`: Studio talks directly to the upstream provider.
- `ExternalAuthentication`: Studio uses Elsa Server's login-method and completion-code contract.
- `ElsaIdentity`: Studio uses the existing direct Elsa local-credential integration.

Mixed direct and brokered registrations fail startup so a deployment cannot silently select the wrong trust model.

## Setting map

| Direct Studio setting | Brokered destination |
| --- | --- |
| `OpenIdConnect:Authority` or `MetadataAddress` | Elsa connection discovery/trust settings |
| `OpenIdConnect:ClientId` | Elsa connection's upstream client ID |
| `OpenIdConnect:ClientSecret` | Elsa connection Secret Binding; supply its value through Elsa Server secret configuration |
| `OpenIdConnect:AuthenticationScopes` | Elsa connection scopes |
| `OpenIdConnect:CallbackPath` | Upstream provider redirects to Elsa's Connection-ID callback |
| `OpenIdConnect:SignedOutCallbackPath` | Upstream provider redirects to Elsa's logout callback when enabled |
| `OpenIdConnect:NameClaimType` and `RoleClaimType` | Elsa claim projection and explicit permission mappings |
| `OpenIdConnect:BackendApiScopes` | Removed from Studio; Elsa issues the credential used for its own API |

Configure a separate Elsa Authentication Client for Studio. It contains Studio's exact callback/logout URI, allowed local return paths, and—for WebAssembly—exact origin. It is not an Elsa API Application and grants no permissions.

## Rollout and rollback

1. Keep `Authentication:Provider` set to `OpenIdConnect`.
2. Configure and test the Elsa broker connection and Studio Authentication Client.
3. Keep the direct and broker secrets in their existing deployment-owned secret stores; no UI or migration process copies them.
4. Change `Authentication:Provider` to `ExternalAuthentication` and restart Studio.
5. Verify sign-in, refresh, API authorization, and logout.

To roll back, restore `Authentication:Provider` to `OpenIdConnect` and restart. The broker does not modify the retained direct settings.

Studio Server uses a confidential broker client, performs exchange server-side, and retains refresh credentials behind an HTTP-only session. Studio WebAssembly uses a public client with mandatory PKCE and memory-only credentials by default; persistent browser storage is opt-in and warned.

The paired Core quickstart contains complete examples: `specs/012-external-authentication/quickstart.md` in the Elsa Core repository.
