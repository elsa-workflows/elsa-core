# Identity, Tenancy, And Security

Elsa security and tenancy are split across identity, SAS token, tenant, tenant HTTP routing, API authorization, and persistence packages.

## Identity

Start in [src/modules/Elsa.Identity](../../src/modules/Elsa.Identity).

[IdentityFeature](../../src/modules/Elsa.Identity/Features/IdentityFeature.cs) registers:

- identity token options
- API key options
- users, applications, and roles options
- memory stores for users, applications, and roles
- user, application, and role providers
- user and role managers
- secret hashing
- access token issuing
- API key generation/parsing
- tenant resolvers based on claims and current user
- FastEndpoints assembly

Identity supports store-based providers, configuration-based providers, and admin bootstrap providers.

## Authentication

[DefaultAuthenticationFeature](../../src/modules/Elsa.Identity/Features/DefaultAuthenticationFeature.cs) wires default authentication. The reference server calls:

```csharp
elsa
    .UseIdentity(...)
    .UseDefaultAuthentication();
```

See [src/apps/Elsa.Server.Web/Program.cs](../../src/apps/Elsa.Server.Web/Program.cs).

Identity JWTs include a `token_use` claim. API bearer authentication accepts only access tokens (`token_use=access`), while `/identity/refresh-token` uses a dedicated refresh-token bearer scheme and accepts only refresh tokens (`token_use=refresh`). Clients should not send refresh tokens to normal API endpoints or access tokens to the refresh endpoint.

JWT signing keys must be configured with a secure random value before production startup. Missing keys, weak keys shorter than 32 ASCII characters, and known public defaults are rejected by options startup validation. Known public defaults are only tolerated in the explicit `Development` or `Demo` environments for local/demo hosts. Use environment variables or a secrets manager, such as `Identity__Tokens__SigningKey` for code-first hosts or `CShells__Shells__Default__Features__Identity__SigningKey` for shell-based hosts.

## Default Admin Bootstrap

The default admin bootstrap is documented in [src/modules/Elsa.Identity/README.md](../../src/modules/Elsa.Identity/README.md) and [ADR 0010](../adr/0010-default-admin-user-bootstrap-for-initial-identity-access.md).

Key points:

- It creates initial admin role/user idempotently.
- It is recommended for initial identity access.
- Do not keep development defaults in production.
- Shell-based configuration uses `DefaultAdminUser` shell feature.
- Code-first configuration uses `identity.UseDefaultAdmin(...)`.

## SAS Tokens

[SasTokensFeature](../../src/modules/Elsa.SasTokens/Features/SasTokensFeature.cs) registers data protection and `ITokenService`. Workflow API depends on SAS tokens. The default data protection application name is `Elsa Workflows`.

Use SAS tokens for protected links or temporary access flows where the API expects signed token semantics.

## Tenancy

Start in [src/modules/Elsa.Tenants](../../src/modules/Elsa.Tenants).

Key features:

- [TenantsFeature](../../src/modules/Elsa.Tenants/Features/TenantsFeature.cs): enables tenant resolution pipeline, tenant options, and tenant resolver services.
- [TenantManagementFeature](../../src/modules/Elsa.Tenants/Features/TenantManagementFeature.cs): registers tenant store, defaulting to memory.
- [TenantManagementEndpointsFeature](../../src/modules/Elsa.Tenants/Features/TenantManagementEndpointsFeature.cs): exposes tenant management endpoints.

Tenant providers:

- configuration-based tenants provider
- store-based tenants provider

The reference server enables configuration-based tenants and a custom tenant resolver pipeline using `CurrentUserTenantResolver`.

## ASP.NET Core Tenant Routing

[Elsa.Tenants.AspNetCore](../../src/modules/Elsa.Tenants.AspNetCore) integrates tenants with HTTP routing.

[MultitenantHttpRoutingFeature](../../src/modules/Elsa.Tenants.AspNetCore/Features/MultitenantHttpRoutingFeature.cs):

- is a dependency of `HttpFeature` and `TenantsFeature`
- configures HTTP endpoint routes and base path providers to use tenant prefixes
- registers route-prefix, header, and host tenant resolvers
- lets hosts configure tenant header and HTTP tenancy options

This feature is important when HTTP workflow routes must be tenant-aware.

## Tenant Persistence Conventions

Persistence is tenant-aware through EF Core model/saving handlers and tenant-aware DbContext factory decoration. ADRs explain conventions:

- [ADR 0008: Empty String As Default Tenant ID](../adr/0008-empty-string-as-default-tenant-id.md)
- [ADR 0009: Asterisk Sentinel Value For Tenant-Agnostic Entities](../adr/0009-asterisk-sentinel-value-for-tenant-agnostic-entities.md)

When changing persisted entities, verify tenant ID behavior and default tenant semantics.

## API Authorization

Workflow API defines read-only-mode authorization in:

- [AuthorizationPolicies](../../src/modules/Elsa.Workflows.Api/Constants/AuthorizationPolicies.cs)
- [NotReadOnlyRequirement](../../src/modules/Elsa.Workflows.Api/Requirements/NotReadOnlyRequirement.cs)

Structured logs define diagnostics permissions in [StructuredLogsPermissions](../../src/modules/Elsa.Diagnostics.StructuredLogs/Permissions/StructuredLogsPermissions.cs).

Identity endpoints and user-management endpoints are permission-based; see [ADR 0010](../adr/0010-default-admin-user-bootstrap-for-initial-identity-access.md).

## Secrets

Start in [src/modules/Elsa.Secrets](../../src/modules/Elsa.Secrets).

[SecretsFeature](../../src/modules/Elsa.Secrets/Features/SecretsFeature.cs) registers secrets services and FastEndpoints. It provides:

- [ISecretManager](../../src/modules/Elsa.Secrets/Contracts/ISecretManager.cs): create, get, rotate, revoke, delete, and test secrets.
- [ISecretResolver](../../src/modules/Elsa.Secrets/Contracts/ISecretResolver.cs): resolve the latest active secret value by immutable technical name.
- [ISecretStore](../../src/modules/Elsa.Secrets/Contracts/ISecretStore.cs) / [ISecretStoreRegistry](../../src/modules/Elsa.Secrets/Contracts/ISecretStoreRegistry.cs): pluggable backend stores.
- [ISecretTypeRegistry](../../src/modules/Elsa.Secrets/Contracts/ISecretTypeRegistry.cs): extensible secret types (text, RSA key, X.509 certificate reference).
- [ISecretRepository](../../src/modules/Elsa.Secrets/Contracts/ISecretRepository.cs): durable secret and version storage.

### Built-In Stores

| Store | Class | Notes |
| --- | --- | --- |
| Elsa-managed encrypted store | [EncryptedSecretStore](../../src/modules/Elsa.Secrets/Stores/EncryptedSecretStore.cs) | Encrypts values with data protection. Default writable store. |
| Configuration-backed read-only | [ConfigurationSecretStore](../../src/modules/Elsa.Secrets/Stores/ConfigurationSecretStore.cs) | Maps configuration keys to secret values. Read-only; cannot be written from the API. |

### Secret Versioning

Each logical secret has an immutable technical name. `ISecretManager.RotateAsync` creates a new active version and retires the previous active version. Runtime code always resolves the latest active version; expired or revoked secrets fail resolution with a non-secret error.

### Management Endpoints

Routes under `/elsa/api` (Elsa route prefix applies):

| Route | Permission |
| --- | --- |
| `GET /secrets` | `read:secrets` |
| `GET /secrets/{name}` | `read:secrets` |
| `POST /secrets` | `write:secrets` |
| `DELETE /secrets/{name}` | `delete:secrets` |
| `POST /secrets/{name}/rotate` | `write:secrets` |
| `POST /secrets/{name}/revoke` | `write:secrets` |
| `POST /secrets/{name}/test` | `test:secrets` |
| `POST /secrets/picker` | `read:secrets` |
| `GET /secrets/descriptors` | `read:secrets` |

Permission constants are in [SecretsPermissions](../../src/modules/Elsa.Secrets/Permissions/SecretsPermissions.cs).

### Using Secrets In Workflows

Activities with sensitive inputs can use the `Secret` expression in place of a literal value. Studio presents this as a no-code picker for inputs marked as sensitive, such as the HTTP Request `Authorization` input. The picker stores a `SecretReference` with the secret name, optional type, and optional scope; it does not store the current secret value in the workflow definition.

At runtime, the `Secret` expression calls `ISecretResolver` at the point of use and returns the latest active version. Rotating a secret through `ISecretManager.RotateAsync` updates future workflow runs without editing workflow JSON. If a reference includes a type or scope, resolution must match those constraints; for example, a text token reference should not resolve an RSA key, and a `production` reference should not resolve a `development` secret. Expired, revoked, missing, or incompatible secrets fail with a non-secret error message.

Sensitive activity inputs are not written to activity state after evaluation. Prefer a `Secret` expression for credentials such as bearer tokens, API keys, passwords, and connection strings instead of literals, variables, logs, workflow outputs, incident messages, or custom headers that are not marked sensitive. Treat any custom activity input that can carry credentials as sensitive by setting `CanContainSecrets = true`.

JavaScript expressions can resolve secrets when the host enables `Elsa.Secrets.JavaScript`:

```javascript
const token = await getSecret("crm:token");
return `Bearer ${token}`;
```

`getSecret(name)` returns a `Promise<string>`, so JavaScript must either `await` it inside an async function/IIFE or compose it with `.then(...)`. Do not write resolved values to logs, variables, outputs, exceptions, or activity state. Use the `Secret` expression for simple no-code binding, and use `getSecret` only when a script needs to combine a secret with runtime data.

### Tests

- [test/unit/Elsa.Secrets.UnitTests](../../test/unit/Elsa.Secrets.UnitTests)
- [test/integration/Elsa.JavaScript.IntegrationTests](../../test/integration/Elsa.JavaScript.IntegrationTests)
- [test/integration/Elsa.Activities.IntegrationTests](../../test/integration/Elsa.Activities.IntegrationTests)

Spec: [specs/007-secrets-module/spec.md](../../specs/007-secrets-module/spec.md).

## Ingress Rate Limiting

Elsa exposes opt-in ASP.NET Core rate limiting hooks for two ingress surfaces:

- Elsa management API endpoints, through `ApiEndpointOptions.RateLimitingPolicyName` and `UseWorkflowsApiRateLimiting(...)`.
- Public HTTP workflow trigger routes, through `HttpActivityOptions.RateLimitingPolicyName` and `UseWorkflowsRateLimiting(...)`.

The reference server registers disabled-by-default fixed-window policies under `IngressRateLimiting`. Enable them by setting `IngressRateLimiting:Enabled` to `true`, then tune the API and HTTP workflow permit/window values for production traffic. Set `IngressRateLimiting:RegisterReferencePolicies` to `false` when policy names and policies are supplied externally. Custom hosts should register named policies with `services.AddRateLimiter(...)`, map Elsa API endpoints with `MapWorkflowsApi(...)`, apply the Elsa metadata hooks after endpoint routing has selected endpoints and before the rate limiter middleware, and call `app.UseRateLimiter()` once in the host pipeline. The Elsa hooks only attach endpoint metadata; ASP.NET Core validates configured policy names when the rate limiter middleware handles matching requests. Leave the option disabled, omit the policy names in custom hosts, or set the reference-server policy options to empty strings to run without Elsa-provided rate limiting. The reference server only assigns its default policy names when `Enabled` is `true`.

## Security Review Checklist

- Does the endpoint require authentication or a permission?
- Do SignalR hub methods enforce authentication and permission checks on every subscribed method, not only at the hub class level?
- Does mutable API behavior honor read-only mode?
- Does the operation need tenant scoping?
- Are exposed API or HTTP workflow trigger routes protected by appropriate ingress rate limiting?
- Does persistence apply tenant ID filters and saving handlers?
- Are bootstrap credentials only for development or secret-managed environments?
- Does any diagnostic/logging feature expose sensitive data without redaction?
- Do token settings use production-grade signing keys and data protection configuration?
