# Implementation Plan: Weaver Copilot UI

**Branch**: `009-weaver-copilot-ui` | **Date**: 2026-06-08 | **Spec**: `specs/009-weaver-copilot-ui/spec.md`
**Input**: Feature specification from `/specs/009-weaver-copilot-ui/spec.md`

## Summary

Add a focused Elsa Studio AI module that exposes Weaver as an operational workspace. Studio discovers Elsa Core AI capabilities and tools, streams chat turns through Core-owned `/ai/chat` SSE events, renders provider-neutral assistant/tool/proposal activity, supports context references, and documents backend gaps for proposal actions, steering, and server-side queueing.

## Technical Context

**Language/Version**: C# / Blazor targeting repository `net10.0` setup  
**Primary Dependencies**: MudBlazor, Refit-style `IBackendApiClientProvider`, Elsa Studio shell/menu abstractions, `HttpClient` SSE streaming  
**Storage**: N/A; Studio keeps only in-memory conversation state and backend-owned identifiers  
**Testing**: xUnit via `dotnet test`  
**Target Platform**: Elsa Studio Blazor Server and Blazor WebAssembly hosts  
**Project Type**: Modular web UI feature  
**Performance Goals**: Stream assistant deltas incrementally; keep long-running activity scannable with capped recent activity rendering  
**Constraints**: No provider SDK references, no browser-to-provider calls, no unsupported proposal/steering/queue API calls  
**Scale/Scope**: One Studio module, one workspace route, provider-neutral client contracts, focused unit/contract-style tests

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Modular Studio Features**: Pass. New feature is isolated under `src/modules/Elsa.Studio.AI` with explicit registration, feature gate, menu item, and route ownership.
- **Backend Capability Awareness**: Pass. Weaver availability and controls are gated through `/ai/capabilities`; discovery handles missing backend endpoints as unavailable state.
- **UX Consistency and Density**: Pass. The route opens directly to a compact workspace with conversation, activity, tools, proposals, and capability status.
- **Async, Disposal, and Real-Time Discipline**: Pass. Chat streaming is async/cancellation-aware, the page cancels active turns on disposal, and background stream updates marshal through `InvokeAsync`.
- **Testing and Verification**: Pass. Reducer, serialization, menu gating, disabled capabilities, reconnect state, streamed events, tools, and proposals are covered by focused tests.
- **Focused Change Sets**: Pass. Changes are scoped to the AI module, host registration, bundle reference, Spec Kit docs, and tests.
- **Simplicity, DRY, and Maintainability**: Pass. The reducer centralizes stream-event state changes and service abstractions remain thin.

## Project Structure

### Documentation (this feature)

```text
specs/009-weaver-copilot-ui/
├── plan.md
├── spec.md
├── quickstart.md
├── data-model.md
├── research.md
├── contracts/
│   └── weaver-api.md
└── tasks.md
```

### Source Code

```text
src/modules/Elsa.Studio.AI/
├── Client/
├── Contracts/
├── Extensions/
├── Menu/
├── Models/
├── Services/
└── UI/

src/modules/Elsa.Studio.AI.Tests/
├── WeaverMenuTests.cs
├── WeaverSerializationTests.cs
└── WeaverWorkspaceReducerTests.cs

src/hosts/Elsa.Studio.Host.Server/Program.cs
src/hosts/Elsa.Studio.Host.Wasm/Program.cs
src/bundles/Elsa.Studio/Elsa.Studio.csproj
Elsa.Studio.sln
```

**Structure Decision**: Use a new `Elsa.Studio.AI` module because Weaver is a user-facing Studio capability with route, menu, services, and backend feature discovery. Host and bundle changes only register the module; backend ownership remains in Elsa Core.

## Complexity Tracking

No constitution violations.
