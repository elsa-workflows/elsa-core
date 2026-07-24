# Implementation Plan: External Authentication

**Branch**: `codex/012-external-authentication` | **Date**: 2026-07-24 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/012-external-authentication/spec.md`

## Summary

Add a server-owned External Authentication broker to Elsa 3. The broker composes configuration-owned and optional database-owned Identity Provider Connections, dispatches to startup-installed Protocol Adapters, resolves normalized External Identities to tenant-owned Elsa Users, composes bounded Elsa permissions, and returns short-lived PKCE-bound completion codes to registered Authentication Clients.

V1 ships OpenID Connect as a separate adapter, optional Elsa Secrets integration, EF persistence integrated with the existing Identity transaction boundary, management and broker APIs, and paired Studio Server/WebAssembly modules. Existing local Identity and direct Studio OpenID Connect contracts remain compatible.

## Technical Context

**Language/Version**: C# latest; nullable reference types and implicit usings; multi-target `net8.0`, `net9.0`, and `net10.0`. Razor components for Studio.

**Primary Dependencies**: Elsa feature/shell infrastructure, Elsa Identity, Elsa Mediator, FastEndpoints, Microsoft IdentityModel OpenID Connect/JWT protocol libraries, ASP.NET Core Data Protection and rate limiting, EF Core Identity persistence, optional Elsa Secrets bridge, Refit, Radzen/MudBlazor, and existing Studio authentication abstractions.

**Storage**: Deployment configuration and in-memory stores support configuration-first/single-node operation. Production persistence extends `IdentityElsaDbContext` for connections, links, sessions, broker transactions, completion grants, and latest observations. Provider-specific migrations cover SQL Server, PostgreSQL, MySQL, SQLite, and Oracle. Multi-node operation requires the shared EF state provider and shared Data Protection configuration.

**Testing**: xUnit unit, EF/integration, and component tests; `WebApplicationFactory` with deterministic fake OpenID Connect provider; Studio unit/server integration tests; Playwright browser tests for WebAssembly; cross-repository contract fixtures.

**Target Platform**: ASP.NET Core Elsa Server; Elsa Studio Blazor Server and Blazor WebAssembly.

**Project Type**: Modular .NET server libraries with REST/browser broker endpoints, optional persistence, client library resources, and sibling Studio Razor class libraries.

**Performance Goals**: 250 ms p95 Login Method discovery at 100 concurrent requests; 500 ms p95 100-row management pages; no more than 250 ms p95 Elsa processing overhead for initiation/callback/exchange excluding provider latency.

**Constraints**: Configuration/database ownership must remain explicit; no token/secret redirect leakage; exact callbacks; mandatory client-to-Elsa S256 PKCE; atomic single-use state; credential-less users; tenant/link uniqueness; no implicit account or permission mapping; no direct-OIDC breakage; public WebAssembly clients contain no secret.

**Scale/Scope**: 10,000 persisted connections across tenants, up to 50 effective Login Methods per tenant, server and WebAssembly clients, three delivery milestones, all supported EF providers, and no continuous health/audit-history subsystem.

## Constitution Check

*GATE: PASS before research and after design.*

| Principle | Verdict | Evidence |
| --- | --- | --- |
| I. Modular Architecture | PASS | Protocol-neutral broker, OpenID Connect adapter, Secrets bridge, persistence integration, and host-specific Studio packages have focused boundaries and communicate through public contracts. |
| II. Composition & Extensibility | PASS | Adapters, connection sources, policies, permission sources/descriptors, secret resolvers, stores, and Studio custom editors are explicit composition points. |
| III. Convention-Driven Design | PASS | Features, shell features, FastEndpoints, stores, descriptors, client resources, and test projects follow repository names and American English. |
| IV. Async & Pipeline Execution | PASS | All provider, store, broker, notification, and HTTP contracts are async and cancellation-aware; cross-module events use Elsa Mediator. |
| V. Testing Discipline | PASS | Unit, integration, component, Studio server, browser, security, and compatibility coverage is part of the task gate. |
| VI. Trunk-Based Development | PASS | One feature branch, focused Core and paired Studio changes, migration documentation, and PR verification are planned. |
| VII. Simplicity, SRP, DRY & KISS | PASS | V1 has one protocol adapter, two built-in admission policies, three grant sources, one latest observation, and no general OAuth server, health history, audit database, or self-linking. |

## Project Structure

### Documentation

```text
specs/012-external-authentication/
├── prd.md
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── rest-api.md
│   ├── runtime-contracts.md
│   └── studio-contract.md
├── checklists/
│   └── requirements.md
└── tasks.md
```

### Elsa Core Repository

```text
src/modules/
├── Elsa.ExternalAuthentication/
│   ├── Contracts/
│   ├── Endpoints/
│   │   ├── Broker/
│   │   ├── Connections/
│   │   ├── Descriptors/
│   │   ├── IdentityLinks/
│   │   ├── Previews/
│   │   └── Sessions/
│   ├── Extensions/
│   ├── Features/
│   ├── Models/
│   ├── Notifications/
│   ├── Options/
│   ├── Permissions/
│   ├── Policies/
│   ├── Providers/
│   ├── Services/
│   ├── ShellFeatures/
│   └── Stores/
├── Elsa.ExternalAuthentication.OpenIdConnect/
│   ├── Extensions/
│   ├── Models/
│   ├── Services/
│   └── Validation/
├── Elsa.ExternalAuthentication.Secrets/
│   ├── Extensions/
│   └── Services/
├── Elsa.Identity/
│   ├── Entities/User.cs
│   ├── Services/DefaultAccessTokenIssuer.cs
│   └── Services/DefaultUserCredentialsValidator.cs
└── Elsa.Persistence.EFCore/
    └── Modules/
        ├── ExternalAuthentication/
        └── Identity/

src/modules/Elsa.Persistence.EFCore.{Sqlite,SqlServer,PostgreSql,MySql,Oracle}/
└── Migrations/Identity/

src/clients/Elsa.Api.Client/Resources/ExternalAuthentication/

test/unit/
├── Elsa.ExternalAuthentication.UnitTests/
└── Elsa.Identity.UnitTests/

test/integration/
└── Elsa.ExternalAuthentication.IntegrationTests/

test/component/Elsa.Workflows.ComponentTests/
└── ExternalAuthentication/
```

### Elsa Studio Repository

```text
src/modules/
├── Elsa.Studio.ExternalAuthentication/
│   ├── Client/
│   ├── Components/
│   ├── Extensions/
│   ├── Menu/
│   ├── Models/
│   ├── Pages/
│   ├── Services/
│   └── Validation/
├── Elsa.Studio.ExternalAuthentication.BlazorServer/
│   ├── Controllers/
│   ├── Extensions/
│   └── Services/
├── Elsa.Studio.ExternalAuthentication.BlazorWasm/
│   ├── Extensions/
│   ├── Pages/
│   └── Services/
└── Elsa.Studio.Security/
    └── Menu/

src/modules/Elsa.Studio.ExternalAuthentication.Tests/
tests/browser/ExternalAuthentication/
```

**Structure Decision**: The Core broker remains protocol-neutral. OpenID Connect proves the deployed-adapter seam; Secrets remains an optional bridge. EF integration extends the current Identity context to make JIT User plus External Identity Link creation atomic. Studio combines chooser and administration in one shared Razor class library with only trust-boundary-specific logic split into Server and WebAssembly packages.

## Phase 0: Research

See [research.md](research.md).

Resolved decisions include:

- Startup-installed adapter packages with runtime-managed connection settings.
- Read-through merged registry with explicit collision/shadowing semantics.
- Atomic state/store contracts and EF Identity transaction integration.
- Opaque completion/external refresh tokens with single-use/rotation.
- Identity token issuance refactoring without breaking `IAccessTokenIssuer`.
- OpenID Connect code-flow and validation through maintained protocol libraries.
- BFF-style Studio Server and public-client Studio WebAssembly profiles.
- Nullable Local Credentials with legacy login/refresh compatibility.
- Explicit permission-delegation and Secret Binding generation boundaries.
- Secure outbound HTTP/rate-limit defaults and one latest test observation.

No `NEEDS CLARIFICATION` markers remain.

## Phase 1: Data Model and Contracts

- [data-model.md](data-model.md)
- [contracts/rest-api.md](contracts/rest-api.md)
- [contracts/runtime-contracts.md](contracts/runtime-contracts.md)
- [contracts/studio-contract.md](contracts/studio-contract.md)
- [quickstart.md](quickstart.md)

## Implementation Sequence

### Milestone 1: Configuration-first Broker Foundation

1. Add protocol-neutral Core module, options, descriptors, registry, in-memory atomic state, broker endpoints, and client registrations.
2. Refactor Identity JWT construction; make Local Credentials optional while retaining legacy endpoints.
3. Add OpenID Connect adapter, outbound HTTP policy, normalized claims, safe errors, rate limits, and fake-provider tests.
4. Add link resolution, reject/JIT policies, grant pipeline, external session/token rotation, and notifications.
5. Add shared Studio module, chooser, and Server/WebAssembly broker host packages.

### Milestone 2: Persisted Administration

1. Extend Identity EF context/migrations and implement atomic connection/link/session/state/observation stores.
2. Add management/descriptor/link/session APIs, permissions, ETags, archive/restore, and Secret Binding bridge.
3. Add connection list/editor, descriptor forms, lifecycle, test, and Preview Sign-in UI.
4. Verify no-restart database changes and authoritative cross-node behavior.

### Milestone 3: Enterprise Hardening

1. Complete host-wide/tenant collision and discovery isolation coverage.
2. Add permission descriptors/delegation warnings, recovery/final-login guard, upstream logout, session administration, and all security notifications.
3. Add cross-node, replay, SSRF, rate-limit, redaction, accessibility, and browser tests.
4. Document direct OpenID Connect migration/rollback, deployment topology, secrets, and operational defaults.

## Post-Design Constitution Re-check

| Principle | Verdict | Post-design evidence |
| --- | --- | --- |
| I. Modular Architecture | PASS | Runtime and Studio contracts isolate broker, adapter, Secrets bridge, persistence, management, and host session responsibilities. |
| II. Composition & Extensibility | PASS | A conformance adapter can participate without schema or client-flow changes; descriptors cover generic UI. |
| III. Convention-Driven Design | PASS | Exact routes, DTOs, feature names, package layout, and endpoint responsibilities are documented. |
| IV. Async & Pipeline Execution | PASS | I/O contracts are cancellation-aware; permission and adapter composition are explicit services. |
| V. Testing Discipline | PASS | Data model and contracts include concurrency, security, cross-node, host-specific, and compatibility test evidence. |
| VI. Trunk-Based Development | PASS | The feature remains one concern and includes required docs and verification. |
| VII. Simplicity, SRP, DRY & KISS | PASS | Shared JWT issuance prevents duplication; one shared Studio UI module avoids premature package fragmentation. |

## Phase 2 Handoff

Generate `tasks.md` with story-oriented phases and explicit Core/Studio repository paths. Every task must cite its covered `FR-*`, `SC-*`, or user story and include tests in the same story phase. Run `/speckit-analyze` before implementation and remediate all critical/high findings.

## Complexity Tracking

No constitution violations require exceptions.
