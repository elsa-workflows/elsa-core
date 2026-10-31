# Codebase Structure

## Top-Level Map

| Path | Purpose | Evidence |
|---|---|---|
| `src/apps/` | Runnable application hosts | `Elsa.sln` |
| `src/common/` | Shared infrastructure | `Elsa.sln` |
| `src/modules/` | Feature and domain modules | `Elsa.sln` |
| `src/clients/` | API client contracts | `src/clients/Elsa.Api.Client/` |
| `test/` | Unit, integration, component, and performance tests | `test/Directory.Build.props` |
| `build/` | NUKE build automation | `build/Build.cs` |
| `specs/` | Feature specifications and plans | `specs/012-weaver-grounding-tools/plan.md` |

## Entry Points

- Main runtime entry points are the application projects under `src/apps/`.
- External Authentication is composed through `AddExternalAuthenticationServices` and optional EF Core replacement registration.
- Endpoint discovery is module-based; identity-link endpoints live under `Elsa.ExternalAuthentication/Endpoints/IdentityLinks`.

## Module Boundaries

| Boundary | Belongs here | Must not be here |
|---|---|---|
| `Elsa.ExternalAuthentication` | Broker orchestration, contracts, in-memory stores, safe HTTP DTOs | Provider-specific protocol behavior |
| `Elsa.ExternalAuthentication.OpenIdConnect` | OIDC callback and provider adapter behavior | Elsa user authorization policy |
| `Elsa.ExternalAuthentication.Persistence.EFCore` | Durable entities and store implementations | UI presentation |
| `Elsa.Api.Client` | Backward-compatible client DTOs and HTTP contracts | Persistence entities |

## Naming and Organization Rules

- C# files and public types use PascalCase.
- Modules are organized by feature, then by contracts/services/endpoints/stores.
- Namespaces follow folders and are file-scoped.

## Evidence

- `Elsa.sln`
- `src/modules/Elsa.ExternalAuthentication/Extensions/ServiceCollectionExtensions.cs`
- `src/modules/Elsa.ExternalAuthentication.Persistence.EFCore/Extensions/ServiceCollectionExtensions.cs`
- `.editorconfig`

