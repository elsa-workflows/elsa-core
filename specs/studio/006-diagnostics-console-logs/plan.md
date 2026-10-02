# Implementation Plan: Diagnostics Console Logs Studio Module

**Branch**: `006-diagnostics-console-logs` | **Date**: 2026-05-18 | **Spec**: [spec.md](./spec.md)  
**Input**: Feature specification from `/specs/006-diagnostics-console-logs/spec.md`

## Summary

Add a new Studio diagnostics module, `Elsa.Studio.Diagnostics.ConsoleLogs`, that exposes a raw stdout/stderr Console page at `/diagnostics/console`. Studio will gate the menu and page on the paired backend console logs remote feature and read permission, load recent line backfill through a typed backend API client, connect to an authenticated SignalR stream for live updates, and render a dense terminal-like viewer with source/stream/text/time filters, URL state, bounded local buffering, copy/export, pause/resume, follow-tail, reconnect, wrapping, compact mode, and raw ANSI display controls.

## Technical Context

**Language/Version**: C# latest and Razor components in the existing multi-targeted Blazor Studio solution.  
**Primary Dependencies**: Elsa Studio module framework, `IBackendApiClientProvider`, `IRemoteFeatureProvider`, `IHttpConnectionOptionsConfigurator`, Refit-style clients, SignalR client, existing Studio component/layout patterns, diagnostics menu conventions from `Elsa.Studio.Diagnostics.StructuredLogs`.  
**Storage**: Client-side bounded in-memory visible row buffer, source metadata, selected filters, connection state, and URL query parameters. No persistent browser storage required.  
**Testing**: xUnit tests for filter/query mapping, row formatting/export formatting, and client service behavior where practical; component/manual verification in the sample host for feature gating, unauthorized/unavailable states, long lines, large buffers, and live updates.  
**Target Platform**: Blazor Server and Blazor WebAssembly Studio hosts.  
**Project Type**: Elsa Studio module package plus bundle/host registration updates.  
**Performance Goals**: Opening `/diagnostics/console` against an enabled local backend displays recent lines and starts live updates within 2 seconds; the viewer remains responsive with 10,000 local console lines through an explicit visible-row cap/pruning strategy.  
**Constraints**: Scope is `elsa-studio` only; do not modify `elsa-core`. Use Studio backend API and SignalR authentication abstractions, hide the navigation item when the backend feature or permission is unavailable, show direct-route unavailable/unauthorized states, dispose live connections on navigation/backend changes, and never parse stdout/stderr into structured log records.  
**Scale/Scope**: One new diagnostics module, one page, API client, SignalR observer, models, filter/query mapping, source-aware viewer controls, tests, docs, and bundle registration. Kubernetes/Docker/orchestrator log API integration and backend-generated exports are outside this slice.

## Constitution Check

Evaluated against `.specify/memory/constitution.md` v1.0.0:

| Principle | Verdict | Evidence |
|-----------|---------|----------|
| I. Modular Studio Features | PASS | The feature is a focused module under `src/modules/Elsa.Studio.Diagnostics.ConsoleLogs` with its own registration, route, menu entry, clients, services, models, and UI. |
| II. Backend Capability Awareness | PASS | The page gates on a paired backend remote feature/permission, uses `IBackendApiClientProvider`, and uses `IHttpConnectionOptionsConfigurator` for SignalR authentication. |
| III. UX Consistency and Density | PASS | The viewer is an operational diagnostics page with dense rows, toolbar controls, URL-preserved filters, and distinct live/error/empty states. |
| IV. Async, Disposal, and Real-Time Discipline | PASS | Recent loading, live streaming, filter updates, reconnect, and disposal are explicit design concerns; local rows are capped. |
| V. Testing and Verification | PASS | The plan includes mapper/service tests and sample-host/manual verification for feature gates, disconnected states, high volume, and long raw text. |
| VI. Focused Change Sets | PASS | Work is scoped to one Studio module plus bundle registration and documentation; no Core or unrelated module changes are planned. |
| VII. Simplicity, DRY, and Maintainability | PASS | The plan reuses StructuredLogs diagnostics patterns and extracts filter/formatting helpers only where duplication becomes structural. |

## Project Structure

### Documentation (this feature)

```text
specs/006-diagnostics-console-logs/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── backend-client.md
│   ├── signalr-client.md
│   └── url-state.md
├── checklists/
│   └── requirements.md
└── tasks.md              # Created by /speckit-tasks, not /speckit-plan
```

### Source Code (repository root)

```text
src/modules/
├── Elsa.Studio.Diagnostics.ConsoleLogs/
│   ├── Elsa.Studio.Diagnostics.ConsoleLogs.csproj
│   ├── Feature.cs
│   ├── _Imports.razor
│   ├── Client/
│   │   └── IConsoleLogsApi.cs
│   ├── Contracts/
│   │   ├── IConsoleLogObserver.cs
│   │   └── IConsoleLogService.cs
│   ├── Extensions/
│   │   └── ServiceCollectionExtensions.cs
│   ├── Menu/
│   │   └── ConsoleLogsMenu.cs
│   ├── Models/
│   │   ├── ConsoleLogConnectionStatus.cs
│   │   ├── ConsoleLogDroppedLineSummary.cs
│   │   ├── ConsoleLogFilter.cs
│   │   ├── ConsoleLogLine.cs
│   │   ├── ConsoleLogSource.cs
│   │   ├── ConsoleLogSourceHealth.cs
│   │   ├── ConsoleLogStream.cs
│   │   └── ConsoleLogViewState.cs
│   ├── Services/
│   │   ├── ConsoleLogExportFormatter.cs
│   │   ├── ConsoleLogFilterMapper.cs
│   │   ├── RemoteConsoleLogService.cs
│   │   └── SignalRConsoleLogObserver.cs
│   ├── UI/Pages/
│   │   ├── ConsoleLogs.razor
│   │   ├── ConsoleLogs.razor.cs
│   │   └── ConsoleLogs.razor.css
│   └── wwwroot/
│       └── consoleLogs.js
├── Elsa.Studio.Diagnostics.ConsoleLogs.Tests/
│   ├── ConsoleLogExportFormatterTests.cs
│   ├── ConsoleLogFilterMapperTests.cs
│   └── Elsa.Studio.Diagnostics.ConsoleLogs.Tests.csproj
└── Elsa.Studio.Diagnostics.StructuredLogs/
    └── unchanged except for coexistence verification

src/bundles/Elsa.Studio/
└── Elsa.Studio.csproj
```

**Structure Decision**: Add a new module beside `Elsa.Studio.Diagnostics.StructuredLogs`. The Console module may reuse public Studio framework and diagnostics menu conventions, but it does not parse raw lines into structured records and does not reach into StructuredLogs internals.

## Phase 0 Output

See [research.md](./research.md).

Resolved decisions:

- Use a dedicated `Elsa.Studio.Diagnostics.ConsoleLogs` module rather than extending StructuredLogs.
- Use `/diagnostics/console` as the Studio route and `Console` as the Diagnostics navigation label.
- Hide the navigation item when the remote feature or permission is unavailable; direct navigation renders a page state.
- Use `/diagnostics/console-logs` REST endpoints and a relative `hubs/diagnostics/console-logs` SignalR hub URL; with the default `/elsa/api` backend base this resolves to `/elsa/hubs/diagnostics/console-logs`.
- Use server-side text filtering for recent backfill and live subscriptions.
- Use stable backend `source.id` for source filters, live subscriptions, URL state, and stale-source selection.
- Preserve URL state through `source`, `stream`, `text`, `from`, `to`, `wrap`, `compact`, `ansi`, and `follow`.

## Phase 1 Output

- [data-model.md](./data-model.md)
- [contracts/backend-client.md](./contracts/backend-client.md)
- [contracts/signalr-client.md](./contracts/signalr-client.md)
- [contracts/url-state.md](./contracts/url-state.md)
- [quickstart.md](./quickstart.md)

## Post-Design Constitution Re-Check

| Principle | Verdict | Post-design evidence |
|-----------|---------|----------------------|
| I. Modular Studio Features | PASS | Artifacts define a standalone ConsoleLogs module, tests project, route, menu, service contracts, and bundle registration. |
| II. Backend Capability Awareness | PASS | Contracts require remote feature/permission gating, authenticated REST, authenticated SignalR, and direct-route unavailable/unauthorized states. |
| III. UX Consistency and Density | PASS | Data model and contracts preserve dense terminal-like display, controls, source health, and explicit loading/live/error states. |
| IV. Async, Disposal, and Real-Time Discipline | PASS | SignalR contract defines start/update/reconnect/unsubscribe/dispose behavior and local buffer caps. |
| V. Testing and Verification | PASS | Quickstart lists targeted test and manual verification paths, including disabled feature, unauthorized, disconnected, large buffer, and long text states. |
| VI. Focused Change Sets | PASS | Planned changes are limited to the new Studio module, tests, bundle registration, and docs. |
| VII. Simplicity, DRY, and Maintainability | PASS | Existing diagnostics patterns are reused; shared helpers are limited to filter mapping and export/row formatting. |

## Phase 2 Handoff

Use `/speckit-tasks` to create the implementation backlog. Suggested implementation order:

1. Add module/test project skeleton, feature registration, bundle reference, menu entry, and route gate.
2. Add models, API client contract, remote service, filter mapper, URL-state mapper, and unit tests.
3. Add SignalR observer with authenticated connection setup, filter updates, reconnect, dropped/source notifications, and disposal tests where practical.
4. Build the Console page with recent loading, live streaming, source/stream/text/time filters, URL state, and explicit page states.
5. Add viewer controls, local row cap, copy/export formatting, raw ANSI/wrap/compact/follow-tail behavior, and representative verification.

## Complexity Tracking

No complexity violations identified.
