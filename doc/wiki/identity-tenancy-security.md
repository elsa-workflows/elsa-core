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

## Security Review Checklist

- Does the endpoint require authentication or a permission?
- Does mutable API behavior honor read-only mode?
- Does the operation need tenant scoping?
- Does persistence apply tenant ID filters and saving handlers?
- Are bootstrap credentials only for development or secret-managed environments?
- Does any diagnostic/logging feature expose sensitive data without redaction?
- Do token settings use production-grade signing keys and data protection configuration?
