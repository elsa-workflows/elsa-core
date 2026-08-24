# Migrate Studio from Direct OpenID Connect to the Elsa Broker

Direct OpenID Connect remains a supported Studio authentication mode. Brokered External Authentication is an explicit alternative: Elsa Server becomes the relying party for upstream providers and issues the Elsa credentials consumed by Studio.

## Choose one mode per Studio host

Set `Authentication:Provider` to exactly one of:

- `OpenIdConnect` for the existing direct integration.
- `ExternalAuthentication` for brokered login.
- `ElsaIdentity` for the existing direct Elsa local-credential integration.

Do not register direct OpenID Connect and brokered authentication in the same host. Ambiguous startup configuration is rejected rather than choosing a mode implicitly.

## Map the existing registration

| Existing `Authentication:OpenIdConnect` setting | Broker destination |
| --- | --- |
| `Authority` / `MetadataAddress` | Configuration-owned connection `adapterSettings.authority` and discovery mode |
| `ClientId` | Connection `adapterSettings.clientId` (the upstream client registration) |
| `ClientSecret` | Connection `secretBindings.clientSecret`; copy the value through deployment secret configuration, never through the API or UI |
| `AuthenticationScopes` | Connection `adapterSettings.scopes` |
| `RequireHttpsMetadata` | Keep secure discovery defaults; any unsafe trust override requires an explicit deployment allowance and privileged confirmation |
| `CallbackPath` | Replace the provider callback with Elsa's fixed Connection-ID callback |
| `SignedOutCallbackPath` | Register Elsa's upstream logout callback when upstream logout is enabled |
| `NameClaimType` / `RoleClaimType` | Configure normalized claim projection and explicit grant mappings; upstream roles are not automatically Elsa permissions |
| `BackendApiScopes` | No direct mapping; Studio calls Elsa with Elsa-issued credentials |

The upstream client and the Elsa Authentication Client are different registrations. The former belongs to the provider connection. The latter identifies Studio to the Elsa broker, grants no Elsa permissions, and owns exact Studio callback, logout callback, origin, and return-path registrations.

## Safe rollout

1. Leave Studio in `OpenIdConnect` mode.
2. Add the External Authentication module to Elsa Server.
3. Add a configuration-owned OpenID Connect connection and a deployment-owned Studio Authentication Client.
4. Register Elsa's provider callback with the upstream provider. Do not remove the existing direct Studio callback yet.
5. Resolve secrets independently in Elsa Server and Studio Server. The migration never moves or reveals a secret.
6. Test the connection and complete a broker login in a non-production environment.
7. Change only `Authentication:Provider` to `ExternalAuthentication` and restart the Studio host.
8. After validation, remove the obsolete direct callback and direct-only secret on your own rotation schedule.

## Rollback

Restore `Authentication:Provider` to `OpenIdConnect` and restart Studio. Because the broker configuration does not rewrite `Authentication:OpenIdConnect` or move its secret, the direct registration remains available until you deliberately retire it.

## Host differences

Studio Server is a confidential Authentication Client. It exchanges the broker code on the server, keeps refresh credentials server-side, and gives the browser a secure HTTP-only session cookie.

Studio WebAssembly is a public Authentication Client. It has no client secret, must use PKCE, and registers an exact browser origin. Its default credential store is memory-only; session or durable browser persistence is an explicit warned deployment choice.

See [`specs/012-external-authentication/quickstart.md`](../../specs/012-external-authentication/quickstart.md) for complete Server and WebAssembly examples.
