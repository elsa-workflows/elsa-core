# Elsa External Authentication

`Elsa.ExternalAuthentication` is the protocol-neutral broker for Elsa-owned external sign-in. It composes deployment-installed provider adapters, connection sources, unlinked-identity policies, permission grant sources, secret resolvers, atomic flow stores, and Elsa token issuance.

OpenID Connect support and the Elsa Secrets bridge are separate optional packages. The broker does not retain provider tokens and does not treat external claims as Elsa permissions unless an explicitly configured grant source maps or bounds them.

## Registration

```csharp
services.AddElsa(elsa =>
{
    elsa.UseExternalAuthentication(feature =>
    {
        feature.ConfigureOptions = options =>
            configuration.GetSection("ExternalAuthentication").Bind(options);
    });
});

services.AddOpenIdConnectExternalAuthentication();
```

`AddExternalAuthenticationServices` supplies in-memory stores suitable for single-node development. A multi-node deployment must replace broker state, grants, sessions, observations, registry versions, and identity links with shared durable implementations, share ASP.NET Core Data Protection keys, and configure the same `HandleHashing:SharedKeyBase64` on every node.

## Configuration ownership

- `ExternalAuthentication:Connections` defines immutable, configuration-owned connections.
- Database-owned connections are optional and controlled by `EnableDatabaseConnections`.
- Configuration takes precedence over a database connection with the same effective key and scope. Studio shows the database row as shadowed instead of silently overwriting it.
- Authentication Clients, extension allowlists, permission boundaries, egress policy, and final-login recovery policy remain deployment-owned.

An empty `AllowedAdapterTypes` collection permits every installed adapter. The built-in policy allowlist contains `reject` and `create-user`; the built-in grant-source allowlist contains `elsa-roles`, `claim-mapping`, `group-mapping`, and `claim-pass-through`.

## Secure defaults

| Setting | Default |
| --- | --- |
| Local broker login | Enabled |
| Database connections | Enabled |
| Unlinked identity policy | `reject` |
| Broker transaction / completion code | 10 minutes / 1 minute |
| Preview / maximum external session | 10 minutes / 8 hours |
| Provider HTTPS | Required |
| Private-network provider destinations | Denied |
| Provider redirects | At most 3, revalidated on every hop |
| Provider request/connect timeout | 10 seconds |
| Broker client PKCE | S256 required |
| WebAssembly credential policy | Memory |
| Upstream logout | Disabled |
| Final-login-path guard | Enabled; recovery method required |
| Session administration | Enabled |
| ASP.NET Core health-check bridge | Disabled |

The separately registered health check is tagged `external-authentication` and `optional`; it is not a readiness dependency by default.

## Secret bindings

The foundation deliberately does not provide a plaintext configuration secret resolver. A secret binding names a resolver type and a reference; a deployment must install a matching `ISecretBindingResolver`. The optional Elsa bridge uses resolver type `elsa-secrets` and resolves active Elsa Secrets by name. Public responses expose only configured/resolvable state.

## Operations

Management, descriptor, link, preview, test, and session APIs are served below `/external-authentication`. On-demand tests store only the latest redacted observation and become stale after a material connection revision. Preview state and results are short-lived, administrator-bound, and one-time; preview never creates a user, link, Elsa credential, or normal session.

See [the full quickstart](../../../specs/012-external-authentication/quickstart.md) and [REST contract](../../../specs/012-external-authentication/contracts/rest-api.md).
