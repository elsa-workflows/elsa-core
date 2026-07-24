# Quickstart: External Authentication

This quickstart demonstrates the configuration-first broker with one OpenID Connect connection. Names are illustrative; exact extension methods follow [runtime-contracts.md](contracts/runtime-contracts.md).

## 1. Register Core Modules

```csharp
services.AddElsa(elsa =>
{
    elsa.UseIdentity(identity =>
    {
        identity.TokenOptions = options =>
        {
            options.SigningKey = configuration["Identity:SigningKey"]!;
            options.Issuer = "https://elsa.example";
            options.Audience = "https://elsa.example";
        };
    });

    elsa.UseExternalAuthentication(feature =>
    {
        feature.ConfigureOptions = options =>
            configuration.GetSection("ExternalAuthentication").Bind(options);
    });
});

services.AddOpenIdConnectExternalAuthentication();
```

Configuration-only/single-node development uses the in-memory atomic state store. A production multi-node host also enables Identity EF persistence for shared external-authentication state, configures shared ASP.NET Core Data Protection keys, and gives every node the same External Authentication handle-hashing key:

```json
{
  "ExternalAuthentication": {
    "HandleHashing": {
      "SharedKeyBase64": "<base64-encoded random value of at least 32 bytes>"
    }
  }
}
```

Generate the value with a cryptographically secure secret generator (for example, `openssl rand -base64 32`) and supply it through the deployment secret/configuration provider rather than source control. Rotating it invalidates outstanding broker transactions and changes persisted external-subject hashes, so rotation requires an explicit migration plan.

Add the optional Elsa Secrets bridge whenever an Authentication Client or identity provider connection uses an `elsa-secrets` Secret Binding:

```csharp
services.AddElsaSecretsExternalAuthentication();
```

The protocol-neutral foundation does not include a plaintext `configuration` secret resolver. A deployment may install its own `ISecretBindingResolver`, or provision active Elsa Secrets with the names used below.

## 2. Register Authentication Clients

```json
{
  "ExternalAuthentication": {
    "AuthenticationClients": [
      {
        "clientId": "elsa-studio-server",
        "displayName": "Elsa Studio Server",
        "clientType": "confidential",
        "callbackUris": [
          "https://studio.example/authentication/external/callback"
        ],
        "logoutCallbackUris": [
          "https://studio.example/authentication/external/logout-callback"
        ],
        "allowedReturnPathPrefixes": ["/"],
        "secretBinding": {
          "resolverType": "elsa-secrets",
          "reference": "external-authentication/studio-server/client-secret",
          "expectedType": "text",
          "expectedScope": "external-authentication"
        },
        "isEnabled": true
      },
      {
        "clientId": "elsa-studio-wasm",
        "displayName": "Elsa Studio WebAssembly",
        "clientType": "public",
        "callbackUris": [
          "https://studio-wasm.example/authentication/external/callback"
        ],
        "logoutCallbackUris": [
          "https://studio-wasm.example/authentication/external/logout-callback"
        ],
        "allowedOrigins": [
          "https://studio-wasm.example"
        ],
        "allowedReturnPathPrefixes": ["/"],
        "isEnabled": true
      }
    ]
  }
}
```

Provision the confidential client secret as the active Elsa Secret named by `secretBinding.reference`, and supply the same value to Studio Server through its deployment secret or key vault. Never commit it:

```text
Authentication__ExternalAuthentication__ClientSecret={strong-random-value}
```

## 3. Configure an OpenID Connect Connection

```json
{
  "ExternalAuthentication": {
    "Connections": [
      {
        "id": "01JZCONTOSOOIDC000000000001",
        "key": "contoso",
        "tenantId": "*",
        "adapterType": "openid-connect",
        "displayName": "Contoso",
        "iconId": "building",
        "displayOrder": 10,
        "isDefault": true,
        "isEnabled": true,
        "adapterSettingsVersion": 1,
        "adapterSettings": {
          "mode": "discovery",
          "authority": "https://login.contoso.example",
          "clientId": "elsa-server",
          "callbackUri": "https://elsa.example/elsa/api/external-authentication/callback/01JZCONTOSOOIDC000000000001",
          "scopes": ["openid", "profile", "email", "groups"],
          "providerPkce": "required"
        },
        "secretBindings": {
          "clientSecret": {
            "resolverType": "elsa-secrets",
            "reference": "external-authentication/contoso/client-secret",
            "expectedType": "text",
            "expectedScope": "external-authentication"
          }
        },
        "unlinkedPolicy": {
          "type": "reject",
          "settingsVersion": 1,
          "settings": {}
        },
        "claimProjection": {
          "allowedClaimTypes": ["name", "email", "groups"],
          "redactedClaimTypes": ["email"],
          "maximumClaimCount": 50,
          "maximumValueLength": 2048,
          "maximumTotalBytes": 32768
        },
        "permissionGrantSources": [
          {
            "type": "elsa-roles",
            "settingsVersion": 1,
            "order": 0,
            "settings": {}
          },
          {
            "type": "group-mapping",
            "settingsVersion": 1,
            "order": 10,
            "settings": {
              "claimType": "groups",
              "mappings": {
                "elsa-workflow-admins": [
                  "read:workflow-definitions",
                  "write:workflow-definitions"
                ]
              }
            }
          }
        ],
        "upstreamLogoutMode": "userChoice"
      }
    ]
  }
}
```

Register this callback with the provider:

```text
https://elsa.example/elsa/api/external-authentication/callback/01JZCONTOSOOIDC000000000001
```

## 4. Configure Studio Server

```json
{
  "Authentication": {
    "Provider": "ExternalAuthentication",
    "ExternalAuthentication": {
      "ClientId": "elsa-studio-server",
      "ClientSecret": "{from-secret-configuration}",
      "CallbackPath": "/authentication/external/callback",
      "LogoutCallbackPath": "/authentication/external/logout-callback"
    }
  }
}
```

Host registration:

```csharp
builder.Services.AddExternalAuthenticationBroker(options =>
    configuration.GetSection("Authentication:ExternalAuthentication").Bind(options));

builder.Services.AddExternalAuthenticationModule(backendApiConfig);
```

The browser receives only the secure Studio cookie. Elsa access and refresh credentials stay in the Studio Server authentication session.

## 5. Configure Studio WebAssembly

```json
{
  "Authentication": {
    "Provider": "ExternalAuthentication",
    "ExternalAuthentication": {
      "ClientId": "elsa-studio-wasm",
      "CallbackPath": "/authentication/external/callback",
      "LogoutCallbackPath": "/authentication/external/logout-callback",
      "BrowserStorage": "Memory"
    }
  }
}
```

No client secret is configured. The default memory token store requires sign-in again after reload. `Session` or `Durable` browser storage is an explicit deployment choice that emits a security warning.

## 6. Verify the Broker

1. Request:

   ```http
   GET https://elsa.example/elsa/api/external-authentication/login-methods?clientId=elsa-studio-wasm
   ```

2. Confirm `contoso` is returned without authority, adapter settings, tenant identifier, client ID, health, remote icon, or secret data.
3. Open Studio `/login`, select Contoso, and complete provider authentication.
4. Confirm the provider redirects only to Elsa's Connection ID callback.
5. Confirm Elsa redirects Studio with only an opaque completion code and client state.
6. Confirm code replay fails.
7. Confirm the Elsa access token has the resolved tenant, session ID, roles, and bounded effective `permissions`.
8. Disable the connection and confirm initiation, pending callback, and external refresh fail while an already-issued access token follows its configured expiry.

## 7. Enable Persisted Administration

Enable the existing Identity EF persistence feature and apply the updated Identity migrations for the chosen provider. The following become shared:

- Database-owned connections and revisions.
- External Identity Links.
- External Authentication Sessions and rotating refresh state.
- Broker transactions and completion grants.
- Latest Connection Observations.

Install `Elsa.Studio.ExternalAuthentication` and navigate to:

```text
/security/external-authentication
```

Create a disabled draft, select OpenID Connect, configure fields and Secret Bindings, validate, test, preview, and enable. Configuration-owned connections remain inspect-only.

Session administration is enabled by default and is available at `/security/external-authentication/sessions` to callers with the session read/revoke permissions. The ASP.NET Core health bridge is deliberately separate and opt-in:

```csharp
services.AddExternalAuthenticationHealthCheck();
```

Map it on a separately selected health endpoint/tag set. It is tagged `external-authentication` and `optional` and does not affect readiness unless the host explicitly includes it there.

## 8. Migrate from Direct Studio OpenID Connect

Suppose Studio Server currently connects directly:

```json
{
  "Authentication": {
    "Provider": "OpenIdConnect",
    "OpenIdConnect": {
      "Authority": "https://login.contoso.example",
      "ClientId": "studio-direct",
      "ClientSecret": "{deployment-secret}",
      "AuthenticationScopes": ["openid", "profile", "email"],
      "CallbackPath": "/signin-oidc",
      "SignedOutCallbackPath": "/signout-callback-oidc"
    }
  }
}
```

Keep that section intact while adding the configuration-owned connection from section 3 and the confidential `elsa-studio-server` Authentication Client from section 2. Register Elsa's Connection-ID callback upstream, test the broker, then switch only:

```json
{
  "Authentication": {
    "Provider": "ExternalAuthentication",
    "ExternalAuthentication": {
      "ClientId": "elsa-studio-server",
      "ClientSecret": "{separate-deployment-secret}",
      "CallbackPath": "/authentication/external/callback",
      "LogoutCallbackPath": "/authentication/external/logout-callback"
    }
  }
}
```

For WebAssembly, the direct configuration similarly remains intact during rollout:

```json
{
  "Authentication": {
    "Provider": "OpenIdConnect",
    "OpenIdConnect": {
      "Authority": "https://login.contoso.example",
      "ClientId": "studio-wasm-direct",
      "AuthenticationScopes": ["openid", "profile", "email"],
      "CallbackPath": "/authentication/login-callback",
      "SignedOutCallbackPath": "/authentication/logout-callback"
    }
  }
}
```

Switch it to the public client from section 2:

```json
{
  "Authentication": {
    "Provider": "ExternalAuthentication",
    "ExternalAuthentication": {
      "ClientId": "elsa-studio-wasm",
      "CallbackPath": "/authentication/external/callback",
      "LogoutCallbackPath": "/authentication/external/logout-callback",
      "BrowserStorage": "Memory"
    }
  }
}
```

The WebAssembly client has no secret and always uses PKCE. Supply upstream and confidential broker secrets explicitly through their deployment secret configurations; never copy them through the API or UI. To roll back either host, restore `Authentication:Provider` to `OpenIdConnect` and restart; the retained direct settings were never changed.

See [the migration guide](../../docs/migrations/external-authentication.md) for the complete setting map and retirement checklist.

## 9. Targeted Verification Commands

Core:

```bash
dotnet test test/unit/Elsa.ExternalAuthentication.UnitTests/Elsa.ExternalAuthentication.UnitTests.csproj
dotnet test test/unit/Elsa.Identity.UnitTests/Elsa.Identity.UnitTests.csproj
dotnet test test/integration/Elsa.ExternalAuthentication.IntegrationTests/Elsa.ExternalAuthentication.IntegrationTests.csproj
dotnet test test/component/Elsa.Workflows.ComponentTests/Elsa.Workflows.ComponentTests.csproj
dotnet build Elsa.sln
```

Studio:

```bash
dotnet test src/modules/Elsa.Studio.ExternalAuthentication.Tests/Elsa.Studio.ExternalAuthentication.Tests.csproj
dotnet build Elsa.Studio.sln
```

Run the browser suite against the deterministic fake provider for both Studio Server and WebAssembly before release.
