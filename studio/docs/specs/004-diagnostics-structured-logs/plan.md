# Implementation Plan: Diagnostics Structured Logs Studio Module

**Branch**: `004-diagnostics-structured-logs` | **Date**: 2026-05-10 | **Spec**: [spec.md](./spec.md)  
**Input**: Feature specification from `/specs/004-diagnostics-structured-logs/spec.md`

## Summary

Refactor the unpublished `Elsa.Studio.ServerLogs` module into `Elsa.Studio.Diagnostics.StructuredLogs`. The module remains a dense operational structured-log viewer, but its identity, route, menu placement, feature gate, API paths, SignalR hub path, public types, tests, docs, and static assets move under the diagnostics structured logs namespace. The page adds semantic structured-log inspection for event ID/name, rendered message, message template, named properties, scopes, exception details, source metadata, trace/span IDs, workflow IDs, tenant, correlation, and raw JSON/copy output.

## Technical Context

**Language/Version**: C# latest and Razor components in the existing multi-targeted Blazor Studio solution.  
**Primary Dependencies**: Elsa Studio module framework, `IBackendApiClientProvider`, `IRemoteFeatureProvider`, `IHttpConnectionOptionsConfigurator`, Refit, SignalR client, MudBlazor/Radzen UI patterns already used by Studio.  
**Storage**: Client-side bounded in-memory row list, selected row/details state, and URL query parameters for shareable filters. No persistent browser storage required.  
**Testing**: Existing xUnit test project for filter mapping, with targeted additions/renames for span ID, event fields, route/query mapping, and structured row formatting where practical. Manual/sample-host verification remains required for UI and live backend behavior.  
**Target Platform**: Blazor Server and Blazor WebAssembly Studio hosts.  
**Project Type**: Elsa Studio module package plus bundle/host registration updates.  
**Performance Goals**: Keep the current bounded row cap behavior; preserve responsive rendering for at least 10,000 received events through pruning and compact display controls.  
**Constraints**: Use Studio backend API and SignalR authentication abstractions, dispose subscriptions on navigation/backend changes, do not modify `elsa-core`, and do not present the module as stdout/stderr console capture.  
**Scale/Scope**: One renamed module, one canonical page, client contracts/services, menu group, source-aware filter UX, workflow-instance link update, tests, docs, and host/bundle references.

## Constitution Check

Evaluated against `.specify/memory/constitution.md` v1.0.0:

| Principle | Verdict | Evidence |
|-----------|---------|----------|
| I. Modular Studio Features | PASS | Refactor keeps the feature in a focused module under `src/modules/Elsa.Studio.Diagnostics.StructuredLogs` with explicit registration, route, services, menu, and models. |
| II. Backend Capability Awareness | PASS | The module gates on `Elsa.Diagnostics.StructuredLogs`, uses `IBackendApiClientProvider`, and configures SignalR with `IHttpConnectionOptionsConfigurator`. |
| III. UX Consistency and Density | PASS | The page remains an operational table/list with toolbar, filters, status states, source selector, compact/wrap controls, and a structured details panel. |
| IV. Async, Disposal, and Real-Time Discipline | PASS | The existing observer lifecycle remains async/disposable and filter updates continue through the live subscription. |
| V. Testing and Verification | PASS | Plan calls out unit tests for filter/model mapping and targeted build/test validation; UI states require manual or sample-host verification. |
| VI. Focused Change Sets | PASS | Work is scoped to one unpublished module identity refactor plus structured-log fields/UX; no Core changes are planned. |
| VII. Simplicity, DRY, and Maintainability | PASS | Prefer direct rename and local helpers for repeated structured details/copy formatting; no new persistence or dashboard framework. |

## Project Structure

### Documentation (this feature)

```text
specs/004-diagnostics-structured-logs/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── backend-client.md
│   └── signalr-client.md
├── checklists/
│   └── requirements.md
└── tasks.md
```

### Source Code (repository root)

```text
src/modules/
├── Elsa.Studio.Diagnostics.StructuredLogs/
│   ├── Elsa.Studio.Diagnostics.StructuredLogs.csproj
│   ├── Feature.cs
│   ├── _Imports.razor
│   ├── Extensions/
│   │   └── ServiceCollectionExtensions.cs
│   ├── Menu/
│   │   └── StructuredLogsMenu.cs
│   ├── Client/
│   │   └── IStructuredLogsApi.cs
│   ├── Contracts/
│   │   ├── IStructuredLogService.cs
│   │   └── IStructuredLogObserver.cs
│   ├── Models/
│   │   ├── StructuredLogEvent.cs
│   │   ├── StructuredLogSource.cs
│   │   ├── StructuredLogFilter.cs
│   │   ├── StructuredLogDetails.cs
│   │   └── StructuredLogConnectionStatus.cs
│   ├── Services/
│   │   ├── RemoteStructuredLogService.cs
│   │   ├── StructuredLogFilterMapper.cs
│   │   └── SignalRStructuredLogObserver.cs
│   ├── UI/Pages/
│   │   ├── StructuredLogs.razor
│   │   └── StructuredLogs.razor.cs
│   └── wwwroot/
│       ├── structuredLogs.css
│       └── structuredLogs.js
├── Elsa.Studio.Diagnostics.StructuredLogs.Tests/
│   ├── Elsa.Studio.Diagnostics.StructuredLogs.Tests.csproj
│   └── StructuredLogFilterMapperTests.cs
└── Elsa.Studio.Workflows/
    └── Components/WorkflowInstanceViewer/Components/WorkflowInstanceDetails.razor.cs

src/bundles/Elsa.Studio/
└── Elsa.Studio.csproj

src/hosts/
├── Elsa.Studio.Host.Server/
└── Elsa.Studio.Host.Wasm/
```

**Structure Decision**: Rename the current module and test project in place conceptually, preserving behavior while replacing the active identity. The Workflows module only links into the new route; diagnostics structured logs does not reach into Workflows internals.

## Phase 0 Output

See [research.md](./research.md).

Resolved decisions:

- Canonical Studio identity is `Elsa.Studio.Diagnostics.StructuredLogs`.
- Canonical remote feature name is `Elsa.Diagnostics.StructuredLogs`.
- Canonical Studio route is `/diagnostics/structured-logs`.
- Canonical REST base path is `/diagnostics/structured-logs`.
- Canonical SignalR hub path is `/elsa/hubs/diagnostics/structured-logs`.
- Studio should add a Diagnostics menu group because the current menu model only defines General and Settings.
- `/server-logs` can be retained only as a development bookmark redirect route if the Razor route model allows it without preserving old active identity.

## Phase 1 Output

- [data-model.md](./data-model.md)
- [contracts/backend-client.md](./contracts/backend-client.md)
- [contracts/signalr-client.md](./contracts/signalr-client.md)
- [quickstart.md](./quickstart.md)

## Post-Design Constitution Re-Check

| Principle | Verdict | Post-design evidence |
|-----------|---------|----------------------|
| I. Modular Studio Features | PASS | Source tree, registration, menu, and route ownership are explicit for the renamed module. |
| II. Backend Capability Awareness | PASS | Contracts and quickstart require feature gating, unavailable states, authenticated REST, and authenticated SignalR. |
| III. UX Consistency and Density | PASS | Details panel augments the existing dense scanner without turning the page into a terminal or marketing view. |
| IV. Async, Disposal, and Real-Time Discipline | PASS | Observer start/update/reconnect/dispose contracts remain explicit. |
| V. Testing and Verification | PASS | Tasks will include renamed mapper tests plus structured field/span coverage and targeted build/test commands. |
| VI. Focused Change Sets | PASS | No unrelated module, dependency, or Core edits are required. |
| VII. Simplicity, DRY, and Maintainability | PASS | Existing service/page boundaries are reused and repeated formatting/copy logic will be extracted only where structural. |

## Phase 2 Handoff

Use [tasks.md](./tasks.md) as the implementation backlog. Suggested implementation order:

1. Rename projects, namespaces, public types, registration, host/bundle references, static assets, docs, and route/menu identity.
2. Update REST and SignalR contracts to diagnostics structured logs names and paths.
3. Expand models, filters, filter mapper, URL state, and tests for event ID/name, message template, span ID, source, tenant, workflow, trace, and correlation fields.
4. Add structured details UI and copy/raw JSON behavior while preserving dense scanner controls.
5. Run targeted builds/tests and fix regressions introduced by the refactor.

## Complexity Tracking

No complexity violations identified.
