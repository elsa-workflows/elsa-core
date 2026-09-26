# Implementation Plan: Server Logs Studio Module

**Branch**: `003-live-server-logs` | **Date**: 2026-05-07 | **Spec**: [spec.md](./spec.md)  
**Input**: Feature specification from `/specs/003-live-server-logs/spec.md`

## Summary

Create a Studio module that appears only when the backend advertises live server log streaming. The module loads recent logs through the backend API, opens an authenticated SignalR subscription for live updates, and presents a dense operational log viewer with filters, source selection, pause/resume, copy, clear, reconnect, auto-scroll, and workflow-instance deep links.

## Technical Context

**Language/Version**: C# latest and Razor components in the existing Blazor Studio solution.  
**Primary Dependencies**: Elsa Studio module framework, `IBackendApiClientProvider`, `IRemoteFeatureProvider`, `IHttpConnectionOptionsConfigurator`, SignalR client, existing MudBlazor/Radzen UI patterns.  
**Storage**: Client-side bounded in-memory row list and URL query parameters for filters. No persistent browser storage required.  
**Testing**: Unit tests for filter mapping and client service behavior where current test infrastructure supports it; component/manual verification in the sample app for UI states and live updates.  
**Target Platform**: Blazor Server and Blazor WebAssembly Studio hosts.  
**Project Type**: Elsa Studio module package.  
**Performance Goals**: Keep UI responsive with 10,000 received events by capping or virtualizing rendered rows; update subscriptions within 500 ms after debounced filter changes.  
**Constraints**: Must use Studio's existing backend authentication and environment switching, dispose SignalR connections reliably, and avoid unbounded local row growth.  
**Scale/Scope**: One module, one page, client contracts/services, menu entry, source-aware filter UX, and optional workflow-instance link integration.

## Constitution Check

Evaluated against `.specify/memory/constitution.md` v1.0.0:

| Principle | Verdict | Evidence |
|-----------|---------|----------|
| I. Modular Studio Features | PASS | New functionality lands in `Elsa.Studio.ServerLogs` with its own feature, menu, route, services, and models. |
| II. Backend Capability Awareness | PASS | Module is remote-feature gated and uses existing API and SignalR authentication abstractions. |
| III. UX Consistency and Density | PASS | Viewer is an operational page with compact rows, toolbar controls, filters, and explicit connection states. |
| IV. Async, Disposal, and Real-Time Discipline | PASS | SignalR observer owns async connection lifecycle and component disposal clears subscriptions. |
| V. Testing and Verification | PASS | Filter mapping, service behavior, unavailable backend, and disposal are called out for tests/verification. |
| VI. Focused Change Sets | PASS | Tasks separate module skeleton, client contracts, observer, UI, workflow integration, and verification. |
| VII. Simplicity, DRY, and Maintainability | PASS | The design starts with one page and bounded local state; virtualization remains an implementation choice if needed. |

## Project Structure

### Documentation (this feature)

```text
specs/003-live-server-logs/
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
└── Elsa.Studio.ServerLogs/
    ├── Elsa.Studio.ServerLogs.csproj
    ├── Feature.cs
    ├── Module.cs
    ├── _Imports.razor
    ├── Extensions/
    │   └── ServiceCollectionExtensions.cs
    ├── Menu/
    │   └── ServerLogsMenu.cs
    ├── Client/
    │   └── IServerLogsApi.cs
    ├── Contracts/
    │   ├── IServerLogService.cs
    │   └── IServerLogObserver.cs
    ├── Models/
    │   ├── ServerLogEvent.cs
    │   ├── ServerLogSource.cs
    │   ├── ServerLogFilter.cs
    │   └── ServerLogConnectionStatus.cs
    ├── Services/
    │   ├── RemoteServerLogService.cs
    │   └── SignalRServerLogObserver.cs
    └── UI/Pages/
        ├── ServerLogs.razor
        └── ServerLogs.razor.cs

src/bundles/Elsa.Studio/
└── Elsa.Studio.csproj # add module reference if bundled by default

src/modules/Elsa.Studio.Workflows/
└── Components/WorkflowInstanceViewer/ # optional contextual link/action
```

**Structure Decision**: Add a dedicated Studio module instead of mixing this into Dashboard or Workflows. Workflow screens may link into it, but the viewer is reusable for non-workflow server diagnostics.

## Phase 0 Output

See [research.md](./research.md).

Resolved decisions:

- Dedicated `Elsa.Studio.ServerLogs` module.
- REST recent-log backfill plus SignalR live stream.
- Compact structured log viewer rather than terminal emulator.
- Source selector defaults to `All sources`.
- Primary filters preserved in URL query string.
- Local row cap first; virtualization only if needed.

## Phase 1 Output

- [data-model.md](./data-model.md)
- [contracts/backend-client.md](./contracts/backend-client.md)
- [contracts/signalr-client.md](./contracts/signalr-client.md)
- [quickstart.md](./quickstart.md)

## Post-Design Constitution Re-Check

| Principle | Verdict | Post-design evidence |
|-----------|---------|----------------------|
| I. Modular Studio Features | PASS | Module boundaries and optional workflow link are explicit. |
| II. Backend Capability Awareness | PASS | Feature detection, unavailable state, API client, and SignalR auth paths are specified. |
| III. UX Consistency and Density | PASS | Toolbar, filters, source state, and empty/error states are specified without introducing a new app shell. |
| IV. Async, Disposal, and Real-Time Discipline | PASS | Observer lifecycle, filter update, and disposal are part of the plan and tasks. |
| V. Testing and Verification | PASS | Tasks include mocked enabled/disabled backend verification and SignalR disposal checks. |
| VI. Focused Change Sets | PASS | Work can be reviewed as module skeleton, services, observer, UI, and workflow context. |
| VII. Simplicity, DRY, and Maintainability | PASS | No persistent client storage or separate dashboard framework is introduced. |

## Phase 2 Handoff

Use [tasks.md](./tasks.md) as the implementation backlog. Suggested PR order:

1. Module skeleton and remote feature gating.
2. REST client contracts and filter models.
3. SignalR observer and connection lifecycle.
4. Server Logs page UX.
5. Workflow-instance contextual link and URL filter preservation.
6. Verification against mocked and live backends.

## Complexity Tracking

No complexity violations identified.
