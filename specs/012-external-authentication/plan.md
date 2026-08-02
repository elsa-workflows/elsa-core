# Implementation Plan: External Authentication

**Status**: Approved baseline with open revision work

**Revised**: 2026-07-24

**Branch**: `codex/012-external-authentication` | **Date**: 2026-07-24 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/012-external-authentication/spec.md`

## Summary

Add a server-owned External Authentication broker to Elsa 3. The broker composes configuration-owned connections, Studio-owned connections, and explicit full-shadow Studio Overrides host-wide within the connected environment. Record IDs identify management and transient broker state; immutable Connection Keys identify durable links and long-lived sessions. The broker dispatches to adapters, evaluates the selected unlinked policy/user matcher, assigns static authorized roles only to newly created users, and returns short-lived PKCE-bound completion codes.

V1 ships OpenID Connect as a separate adapter, Managed Secrets through Elsa Secrets, External Secrets through standard configuration, EF persistence integrated with the existing Identity transaction boundary, management and broker APIs, a generic `Elsa.Studio.Authentication.UI` shell, and paired Studio Server/WebAssembly clients. Existing local Identity and direct Studio OpenID Connect contracts remain compatible throughout Elsa 3.x.

## Technical Context

**Language/Version**: C# latest; nullable reference types and implicit usings; multi-target `net8.0`, `net9.0`, and `net10.0`. Razor components for Studio.

**Primary Dependencies**: Elsa feature/shell infrastructure, Elsa Identity, Elsa Mediator, FastEndpoints, Microsoft IdentityModel OpenID Connect/JWT protocol libraries, ASP.NET Core Data Protection and rate limiting, EF Core Identity persistence, optional Elsa Secrets bridge, Refit, Radzen/MudBlazor, and existing Studio authentication abstractions.

**Storage**: Deployment configuration and in-memory stores support configuration-first/single-node operation. Production persistence uses a dedicated `ExternalAuthenticationElsaDbContext` for connections, links, sessions, broker transactions, completion grants, and latest observations, enabled through the `<Provider>ExternalAuthenticationPersistence` shell feature. Provider-specific migrations cover SQL Server, PostgreSQL, MySQL, SQLite, and Oracle. Multi-node operation requires the shared EF state provider and shared Data Protection configuration.

**Testing**: xUnit unit, EF/integration, and component tests; `WebApplicationFactory` with deterministic fake OpenID Connect provider; Studio unit/server integration tests; Playwright browser tests for WebAssembly; cross-repository contract fixtures.

**Target Platform**: ASP.NET Core Elsa Server; Elsa Studio Blazor Server and Blazor WebAssembly.

**Project Type**: Modular .NET server libraries with REST/browser broker endpoints, optional persistence, client library resources, and sibling Studio Razor class libraries.

**Performance Goals**: 250 ms p95 Login Method discovery at 100 concurrent requests; 500 ms p95 100-row management pages; no more than 250 ms p95 Elsa processing overhead for initiation/callback/exchange excluding provider latency.

**Constraints**: Host-wide administration in the connected environment without a target field; record-ID/logical-key separation; complete overrides with disabled-shadow/archive-reveal semantics; exact `discoveryUrl` as the safe default with separately authorized and deployment-gated Advanced overrides for discovery-derived issuer/endpoints/signing keys; immutable deployment-derived callbacks, confidential upstream OIDC, S256 PKCE, and protocol validation; basic/post client authentication; ephemeral user-matcher claims; no direct claim-permission/role mapping; no Direct OIDC breakage.

**Scale/Scope**: 10,000 host-wide persisted/override records, up to 50 effective Login Methods, server and WebAssembly clients, all supported EF providers, and no continuous health/audit-history subsystem.

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
        └── Identity/

src/modules/Elsa.ExternalAuthentication.Persistence.EFCore/
├── ExternalAuthenticationElsaDbContext.cs
├── Entities.cs
├── Configurations.cs
├── Stores/
├── Features/
└── ShellFeatures/

src/modules/Elsa.ExternalAuthentication.Persistence.EFCore.{Sqlite,SqlServer,PostgreSql,MySql,Oracle}/
└── Migrations/ExternalAuthentication/

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

**Structure Decision**: The Core broker remains protocol-neutral. OpenID Connect proves the adapter seam; Elsa Secrets and the configuration resolver cover Managed and External ownership. EF integration extends the Identity context so JIT User, link, and authorized role assignment are atomic. `Elsa.Studio.Authentication.UI` owns the generic shell; External Authentication contributes login behavior and connection administration. Host-specific credential handling remains split into Server and WebAssembly packages.

## Phase 0: Research

See [research.md](research.md).

Resolved decisions include:

- Startup-installed adapter packages with runtime-managed connection settings.
- Read-through merged registry with explicit full-shadow override semantics.
- Atomic state/store contracts and EF Identity transaction integration.
- Opaque completion/external refresh tokens with single-use/rotation.
- Identity token issuance refactoring without breaking `IAccessTokenIssuer`.
- OpenID Connect code-flow and validation through maintained protocol libraries.
- BFF-style Studio Server and public-client Studio WebAssembly profiles.
- Nullable Local Credentials with legacy login/refresh compatibility.
- Single External User Matcher policy semantics, static create-user role authorization, and Managed/External Secret Binding boundaries.
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
4. Add durable link resolution by Connection Key, reject/create/matcher policies, atomic static create-user roles, external session/token rotation, and notifications.
5. Add the Authentication.UI shell, external-auth contributions, and Server/WebAssembly broker host packages.

### Milestone 2: Persisted Administration

1. Extend Identity EF context/migrations and implement atomic connection/link/session/state/observation stores.
2. Add management/descriptor/link/session APIs, explicit full-shadow overrides, permissions, ETags, archive/restore, and Managed/External Secret resolvers.
3. Add connection list/editor, descriptor forms, lifecycle, test, and Preview Sign-in UI.
4. Verify no-restart database changes and authoritative cross-node behavior.

### Milestone 3: Enterprise Hardening

1. Complete host-wide environment, record-ID/logical-key, override lifecycle, and discovery coverage.
2. Add user-matcher/fallback and static role warnings, recovery/final-login guard, minimal-token upstream logout, session administration, and notifications.
3. Add cross-node, replay, SSRF, rate-limit, redaction, accessibility, and browser tests.
4. Document Direct OIDC staged deprecation/migration/rollback, deployment topology, secrets, and operational defaults.

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
