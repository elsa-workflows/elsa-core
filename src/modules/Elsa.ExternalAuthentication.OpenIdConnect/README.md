# Elsa External Authentication: OpenID Connect

This package installs the `openid-connect` adapter for `Elsa.ExternalAuthentication`.

```csharp
services.AddOpenIdConnectExternalAuthentication();
```

The adapter uses the authorization-code flow, validates issuer, signature, audience/authorized party, expiry, nonce, and callback state, and enables upstream S256 PKCE by default. It projects only connection-allowlisted claims and never returns provider tokens from broker or management APIs.

## Settings

| Setting | Required | Description |
| --- | --- | --- |
| `authority` | Yes | Absolute HTTPS provider authority. |
| `clientId` | Yes | Upstream provider client registration. |
| `callbackUri` | Yes | Exact absolute HTTPS Elsa callback registered with the provider. |
| `mode` | Yes | `discovery` or `manual`; discovery is the recommended default. |
| `scopes` | No | Requested scopes; `openid` is always included. |
| `providerPkce` | Yes | `required` (default) or privileged unsafe `disabled`. |
| `clientSecret` | No | Secret Binding field, never a value inside adapter settings. |
| `useUserInfo` | No | Fetch UserInfo after ID-token validation. |
| `userInfoEndpoint` | No | Optional explicit HTTPS UserInfo endpoint. |
| `endSessionEndpoint` | No | Optional explicit HTTPS upstream logout endpoint. |

Manual trust additionally requires `issuer`, `authorizationEndpoint`, and `tokenEndpoint`, plus either `jwksUri` or pinned `signingKeys`. Manual trust fields and disabling provider PKCE are unsafe settings that require the corresponding privileged Studio confirmation.

For a callback with the default Elsa API prefix:

```text
https://elsa.example/elsa/api/external-authentication/callback/{connection-id}
```

The configured `callbackUri` and the URI registered upstream must match exactly.
