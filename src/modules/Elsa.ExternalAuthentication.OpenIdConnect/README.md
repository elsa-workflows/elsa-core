# Elsa External Authentication: OpenID Connect

This package installs the `openid-connect` adapter for `Elsa.ExternalAuthentication`.

```csharp
services.AddOpenIdConnectExternalAuthentication();
```

The adapter uses the authorization-code flow, validates issuer, signature, audience/authorized party, expiry, nonce, and callback state, and always uses upstream S256 PKCE. It projects only connection-allowlisted claims and never returns provider tokens from broker or management APIs. An ID-token logout hint is retained only in protected server-side session state when upstream logout is enabled.

## Settings

| Setting | Required | Description |
| --- | --- | --- |
| `discoveryUrl` | Discovery mode | Exact absolute HTTPS OpenID Connect discovery document URL. |
| `clientId` | Yes | Upstream provider client registration. |
| `clientAuthenticationMethod` | Yes | `client_secret_basic` (default) or `client_secret_post`. |
| `mode` | Yes | `discovery` or `manual`; discovery is the recommended default. |
| `scopes` | No | Requested scopes; `openid` is always included. |
| `providerPkce` | No | Compatibility marker; S256 PKCE is always required. |
| `clientSecret` | Yes | Required Secret Binding field, never a value inside adapter settings. |
| `endSessionEndpoint` | No | Optional explicit HTTPS upstream logout endpoint. |

Manual trust additionally requires `issuer`, `authorizationEndpoint`, and `tokenEndpoint`, plus either `jwksUri` or pinned `signingKeys`.

For a callback with the default Elsa API prefix:

```text
https://elsa.example/elsa/api/external-authentication/callback/{connection-key}
https://elsa.example/elsa/api/external-authentication/previews/callback/{connection-id}
```

The normal callback uses the immutable logical connection key; the administrator-preview callback uses the stable connection record ID. Both are derived from the deployment-owned `Redirects:ExternalCallbackBaseUri`, are not editable per connection, and must be registered upstream exactly when Preview is enabled for administrators. Management responses and Studio display both derived values.
