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

The foundation includes the `configuration` resolver, which reads deployment-owned secrets from standard .NET configuration. It is registered automatically by `AddExternalAuthenticationServices`; no additional package or custom `ISecretBindingResolver` is required. A binding contains only the configuration key, never the secret value:

```json
{
  "ExternalAuthentication": {
    "AuthenticationClients": [
      {
        "clientId": "elsa-studio-server",
        "displayName": "Elsa Studio Server",
        "clientType": "confidential",
        "callbackUris": [
          "https://localhost:7113/authentication/external/callback"
        ],
        "logoutCallbackUris": [
          "https://localhost:7113/authentication/external/logout-callback"
        ],
        "allowedReturnPathPrefixes": ["/"],
        "secretBinding": {
          "ownership": "external",
          "resolverType": "configuration",
          "reference": "Secrets:ExternalAuthentication:StudioServerClientSecret"
        },
        "isEnabled": true
      }
    ]
  },
  "Secrets": {
    "ExternalAuthentication": {
      "StudioServerClientSecret": "<development-secret>"
    }
  }
}
```

The equivalent environment variable is:

```text
Secrets__ExternalAuthentication__StudioServerClientSecret=<deployment-secret>
```

Any standard `IConfiguration` provider can supply the referenced value, including environment variables, Kubernetes-mounted configuration, and cloud secret providers. In a CShells feature block, feature settings are exposed under the feature name; for example, a value nested at `Features:ExternalAuthentication:Secrets:StudioServerClientSecret` is referenced as `ExternalAuthentication:Secrets:StudioServerClientSecret`. After removing the value from `appsettings.json`, its environment-variable equivalent is `ExternalAuthentication__Secrets__StudioServerClientSecret`.

Configuration bindings use `ownership: external`: their values and lifecycle remain deployment-owned and read-only in Studio. The optional Elsa Secrets bridge instead uses `ownership: managed` with resolver type `elsa-secrets`; it resolves active Elsa Secrets by name and allows authorized administrators to manage their lifecycle through Elsa. Public responses expose only configured/resolvable state for either binding type and never return secret values.

## Operations

Management, descriptor, link, preview, test, and session APIs are served below `/external-authentication`. On-demand tests store only the latest redacted observation and become stale after a material connection revision. Preview state and results are short-lived, administrator-bound, and one-time; preview never creates a user, link, Elsa credential, or normal session.

See [the full quickstart](../../../specs/012-external-authentication/quickstart.md) and [REST contract](../../../specs/012-external-authentication/contracts/rest-api.md).
