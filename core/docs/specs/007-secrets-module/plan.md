# Implementation Plan: Secrets Module

**Branch**: `007-secrets-module` | **Date**: 2026-05-19 | **Spec**: [spec.md](./spec.md)  
**Input**: Feature specification from `/specs/007-secrets-module/spec.md`

## Summary

Introduce a first-class `Elsa.Secrets` Core/server module that promotes and redesigns the existing `elsa-extensions` secrets work into an Orchard-style system of immutable named secrets, latest-active resolution, pluggable stores, extensible secret types, safe management APIs, import/export contracts, and Studio picker UX. V1 includes an Elsa-managed encrypted store, a configuration-backed read-only store, and paired management/picker UI in `elsa-studio`.

## Technical Context

**Language/Version**: C# latest, nullable reference types enabled, implicit usings enabled.  
**Primary Dependencies**: Elsa feature/module infrastructure, FastEndpoints through Elsa API endpoint patterns, existing Elsa identity/authorization patterns, Elsa workflow input metadata, `Microsoft.Extensions.Configuration`, `Microsoft.AspNetCore.DataProtection`, EF Core persistence infrastructure, mediator notifications, and optional JavaScript expression integration.  
**Storage**: In-memory store for tests/development; Elsa-managed encrypted store with EF Core persistence for production; configuration-backed read-only store for deployment-managed values. No cloud vault or OS certificate store provider in v1.  
**Testing**: xUnit unit tests for name validation, version lifecycle, store/type capability checks, resolution, no-reveal behavior, configuration store behavior, import/export conflict handling, and API model safety; integration tests for endpoints, permissions, EF Core store behavior, shell feature registration, and migration/adapters from the existing extension model.  
**Target Platform**: ASP.NET Core Elsa Server on the repository's supported .NET target frameworks.  
**Project Type**: Modular .NET server library with REST API endpoints, persistence providers, and Studio-facing contracts.  
**Performance Goals**: Resolve latest-active in-process or database-backed secrets within 50 ms p95 for local store reads under normal server load; list/filter metadata pages within 250 ms p95 for 10,000 secrets with indexed fields; avoid unbounded memory growth in import/export and listing flows.  
**Constraints**: Secret technical names are immutable; no cleartext reveal after creation; references resolve latest active version only; same-name import conflicts require explicit operator choice; v1 store scope is limited to Elsa-managed encrypted and configuration-backed read-only stores.  
**Scale/Scope**: Core module, management/runtime contracts, API endpoints, two v1 store implementations, secret type registry and three built-in types, import/export contracts, adapter/migration path for `elsa-extensions`, shell feature registration, Studio management/picker UX, documentation, and targeted tests.

## Constitution Check

Evaluated against `.specify/memory/constitution.md` v1.1.0:

| Principle | Verdict | Evidence |
|-----------|---------|----------|
| I. Modular Architecture | PASS | Secrets is a focused module under `src/modules/` with optional persistence provider packages and published contracts for cross-module use. |
| II. Composition & Extensibility | PASS | Secret stores, secret types, value protection, import/export handlers, and picker contexts are explicit extension points. |
| III. Convention-Driven Design | PASS | Endpoints follow single-endpoint classes; features, shell features, stores, contracts, and tests follow repository naming patterns and American English. |
| IV. Async & Pipeline Execution | PASS | Store, resolution, API, import/export, and provider contracts are async and cancellation-aware. |
| V. Testing Discipline | PASS | Plan calls for unit and integration coverage of lifecycle, security, APIs, stores, migration, and shell registration. |
| VI. Trunk-Based Development | PASS | Server/Core and Studio work are isolated to their respective repositories. |
| VII. Simplicity, SRP, DRY & KISS | PASS | V1 limits concrete stores to two, avoids cleartext reveal, and defers cloud vault/certificate providers until contracts prove stable. |

## Project Structure

### Documentation (this feature)

```text
specs/007-secrets-module/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── runtime-contract.md
│   ├── rest-api.md
│   ├── studio-contract.md
│   └── import-export.md
├── checklists/
│   └── requirements.md
└── tasks.md
```

### Source Code (repository root)

```text
src/modules/
├── Elsa.Secrets/
│   ├── Contracts/
│   ├── Endpoints/Secrets/
│   ├── Extensions/
│   ├── Features/
│   ├── Models/
│   ├── Notifications/
│   ├── Permissions/
│   ├── Providers/Configuration/
│   ├── Providers/InMemory/
│   ├── Services/
│   ├── ShellFeatures/
│   └── UIHints/
├── Elsa.Secrets.Persistence.EFCore/
│   ├── Configurations/
│   ├── Extensions/
│   ├── Features/
│   ├── Migrations/
│   ├── Services/
│   └── ShellFeatures/
├── Elsa.Secrets.Persistence.EFCore.PostgreSql/
├── Elsa.Secrets.Persistence.EFCore.Sqlite/
└── Elsa.Secrets.Persistence.EFCore.SqlServer/

test/unit/
└── Elsa.Secrets.UnitTests/

test/integration/
└── Elsa.Secrets.IntegrationTests/
```

**Structure Decision**: Add `Elsa.Secrets` as the Core/server contract, API, runtime, and in-memory/configuration-store module. Add EF Core persistence as optional provider packages so production encrypted storage follows Elsa persistence conventions without forcing database dependencies into the core module. Add the paired `Elsa.Studio.Secrets` module in the sibling Studio repository.

## Phase 0 Output

See [research.md](./research.md).

Resolved decisions:

- Promote the existing extension module by redesigning its boundaries instead of copying it as-is.
- Use immutable technical names as serialized secret references.
- Resolve references to latest active versions only.
- Disallow user-initiated cleartext reveal after creation.
- Keep v1 stores limited to Elsa-managed encrypted and configuration-backed read-only stores.
- Keep external store providers out of the first server/Core delivery; include the paired Studio management and picker UX.

## Phase 1 Output

- [data-model.md](./data-model.md)
- [contracts/runtime-contract.md](./contracts/runtime-contract.md)
- [contracts/rest-api.md](./contracts/rest-api.md)
- [contracts/studio-contract.md](./contracts/studio-contract.md)
- [contracts/import-export.md](./contracts/import-export.md)
- [quickstart.md](./quickstart.md)

## Post-Design Constitution Re-Check

| Principle | Verdict | Post-design evidence |
|-----------|---------|----------------------|
| I. Modular Architecture | PASS | Data model and contracts separate core runtime, API, persistence, and Studio-facing concerns. |
| II. Composition & Extensibility | PASS | Store/type/provider capability contracts allow later vault and certificate providers without changing consumer references. |
| III. Convention-Driven Design | PASS | REST, model, feature, permission, and test names are aligned with existing Elsa modules. |
| IV. Async & Pipeline Execution | PASS | Contracts use async APIs and cancellation tokens for all I/O and runtime resolution. |
| V. Testing Discipline | PASS | Quickstart and contracts identify targeted unit and integration verification paths. |
| VI. Trunk-Based Development | PASS | Core/server and Studio changes remain separated by repository boundary. |
| VII. Simplicity, SRP, DRY & KISS | PASS | No reveal path, no version pinning, and no cloud vault implementation in this first slice. |

## Phase 2 Handoff

Use `/speckit-tasks` to generate the implementation backlog. Suggested order:

1. Create module and test project skeletons with feature, shell feature, permission, and endpoint registration.
2. Implement models, immutable-name validation, lifecycle services, in-memory store, configuration store, and value protection boundaries.
3. Add REST API contracts/endpoints and no-reveal response models.
4. Add EF Core persistence packages and migrations for supported providers.
5. Add import/export and existing-extension adapter/migration contracts.
6. Add Studio-facing picker/type/store contracts, Studio management/picker UX, and documentation.
7. Add unit/integration tests and quickstart validation.

## Complexity Tracking

No constitution violations identified.
