# Architecture

## Architectural Style

- Primary style: modular, feature-oriented ASP.NET Core application with dependency-injected ports and adapters.
- Classification evidence: External Authentication defines protocol-neutral contracts and broker orchestration, while OIDC and EF Core are separate adapter modules.
- Primary constraints: tenant isolation, non-reversible subject storage, safe public errors, and replaceable in-memory/durable stores.

## System Flow

```text
provider callback -> protocol adapter -> external identity resolver -> session/grant stores -> identity-link activity write -> safe redirect/API
```

1. The callback endpoint delegates to `ExternalAuthenticationBroker.CompleteCallbackAsync`.
2. The configured adapter validates the provider callback and returns a normalized `ExternalIdentity`.
3. `DefaultExternalIdentityResolver` finds or atomically provisions the tenant-scoped link and Elsa user.
4. The broker resolves grants and persists the external session and one-time completion grant.
5. The provisioner records the successful sign-in on the same tenant/connection/issuer/subject/user tuple.
6. Identity-link list endpoints map safe fields, including `LastSignedInAt`, without returning subject hashes.

## Layer/Module Responsibilities

| Module | Owns | Must not own | Evidence |
|---|---|---|---|
| Broker | Successful authentication orchestration and completion boundary | Provider token validation details | `ExternalAuthenticationBroker.cs` |
| Resolver/provisioner | Link lookup, JIT creation, tuple isolation, activity persistence | Browser redirects | `DefaultExternalIdentityResolver.cs`, `IExternalIdentityProvisioner` |
| EF Core adapter | Durable entity mapping and atomic compare-and-set updates | API response shaping | `EFCoreExternalIdentityProvisioner.cs` |
| Identity-links endpoint | Tenant-scoped filters and safe response mapping | Raw external subjects | `IdentityLinkEndpoints.cs` |

## Reused Patterns

| Pattern | Where | Purpose |
|---|---|---|
| Port/adapter | `IExternalAuthenticationAdapter`, OIDC implementation | Isolate protocols |
| Repository/provisioner | In-memory and EF external identity provisioners | Replace persistence without changing broker behavior |
| DI replacement | EF Core service collection extension | Replace single-node defaults with durable stores |
| Safe DTO mapping | Identity-link endpoint document | Prevent subject/hash disclosure |

## Known Architectural Risks

- Session, completion grant, and link activity are separate store writes; there is no cross-store transaction.
- Custom resolver/provisioner pairs can opt into `IExternalIdentitySignInTracker`; the built-in pair implements it.

## Evidence

- `src/modules/Elsa.ExternalAuthentication/Services/ExternalAuthenticationBroker.cs`
- `src/modules/Elsa.ExternalAuthentication/Services/DefaultExternalIdentityResolver.cs`
- `src/modules/Elsa.ExternalAuthentication.Persistence.EFCore/Stores/EFCoreExternalIdentityProvisioner.cs`
- `src/modules/Elsa.ExternalAuthentication/Endpoints/IdentityLinks/IdentityLinkEndpoints.cs`
