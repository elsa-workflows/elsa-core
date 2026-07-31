# Implementation Plan: Extensible Activity Output Converters

**Branch**: `012-output-converters` | **Date**: 2026-07-30 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/012-output-converters/spec.md`

## Summary

Add an explicit, synchronous conversion boundary between an activity's native output and its bound variable or workflow output. Elsa Core will own persisted converter configuration, a descriptor registry, scoped converter invocation, validation, and privacy-safe faults; Elsa's API and client will expose safe discovery contracts; Studio will filter, configure, validate, and round-trip converter settings. Unconfigured bindings retain the existing hot path.

## Technical Context

**Language/Version**: C# latest; .NET 8.0, 9.0, and 10.0 in Core; .NET 10.0 Blazor in Studio

**Primary Dependencies**: System.Text.Json, Microsoft.Extensions.DependencyInjection keyed services, JsonSchema.Net, FastEndpoints, Refit, MudBlazor

**Storage**: Existing workflow-definition JSON and persisted workflow exception state; no new database entity or migration

**Testing**: xUnit, Elsa shared workflow fixtures, component/integration tests, bUnit for Studio

**Target Platform**: Elsa server library and web API on supported .NET platforms; Elsa Studio web application

**Project Type**: Multi-project library, web API/client, and separate Blazor Studio repository

**Performance Goals**: Zero converter lookup or converter-related allocation for unconfigured bindings; no more than 2% representative assignment-throughput regression

**Constraints**: Synchronous deterministic conversion, no mutable workflow context in converters, no native values/settings in default faults, backward-compatible omitted JSON, multi-target Core support

**Scale/Scope**: Hundreds of registered descriptors, one optional converter per binding, small JSON settings documents

## Constitution Check

- **Modular Architecture — PASS**: Runtime contracts and behavior stay in Workflows Core, definition validation in Management, discovery endpoints in Workflows API, client contracts in Elsa.Api.Client, and authoring behavior in Studio.
- **Composition & Extensibility — PASS**: Public converter registration uses DI and explicit descriptors; modules own production converter semantics.
- **Convention-Driven Design — PASS**: Existing Output wrappers, FastEndpoints structure, Refit resources, nullable annotations, and American English are retained.
- **Async & Pipeline Execution — PASS**: Conversion is intentionally synchronous and in-memory; faults continue through the existing activity execution pipeline.
- **Testing Discipline — PASS**: Unit, integration/component, API, serialization, and bUnit coverage are required.
- **Trunk-Based Development — PASS**: One feature branch and one concern; public API documentation is included.
- **Simplicity, SRP, DRY & KISS — PASS**: One registry, one invoker, one destination resolver, and one settings validator divide responsibilities without introducing converter chains or generic coercion.

Post-design re-check: PASS. The new JSON Schema dependency is justified by a standards-compliant validation requirement; an ad hoc subset was rejected.

## Project Structure

### Documentation

```text
specs/012-output-converters/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── rest-api.md
│   ├── runtime-contract.md
│   └── studio-contract.md
├── checklists/
│   └── requirements.md
└── tasks.md
```

### Elsa Core repository

```text
src/modules/Elsa.Workflows.Core/
├── Contracts/
├── Contexts/ActivityExecutionContext.cs
├── Enums/
├── Exceptions/
├── Extensions/
├── Models/
├── Serialization/
└── Services/

src/modules/Elsa.Workflows.Management/
└── Handlers/Notifications/

src/modules/Elsa.Workflows.Api/
└── Endpoints/OutputConverters/List/

src/clients/Elsa.Api.Client/
├── Resources/OutputConverters/
└── Shared/Models/ActivityOutput.cs

test/
├── unit/Elsa.Workflows.Core.UnitTests/
├── unit/Elsa.Workflows.Management.UnitTests/
├── unit/Elsa.Workflows.Api.UnitTests/
└── component/Elsa.Workflows.ComponentTests/
```

### Elsa Studio repository

```text
src/modules/Elsa.Studio.Workflows.Core/
├── Domain/Contracts/
├── Domain/Services/
└── Extensions/

src/modules/Elsa.Studio.Workflows/
└── Components/WorkflowDefinitionEditor/Components/ActivityProperties/Tabs/Outputs/

src/modules/Elsa.Studio.Workflows.Tests/
└── OutputConverters/
```

**Structure Decision**: Extend the existing module and resource boundaries. No new production project is needed; the feature is a Core extensibility point consumed by existing Management, API, client, and Studio modules.

## Implementation Strategy

1. Add persisted configuration and public runtime contracts with backward-compatible serialization.
2. Add strict registration, descriptor lookup, scoped invocation, binder-specific destination writes, and privacy-safe structured faults.
3. Add publication/import validation using the same descriptor, destination, settings, and compatibility rules as runtime safety checks.
4. Add descriptor discovery API/client contracts and authorization.
5. Add Studio remote discovery, compatibility-aware authoring, settings editing, and round-trip tests.
6. Harden version-skew, synthetic-output, replay, default-path performance, and public documentation.

## Complexity Tracking

No constitution violations require justification.
