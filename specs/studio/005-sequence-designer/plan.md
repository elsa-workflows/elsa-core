# Implementation Plan: Sequence Designer

**Branch**: `005-sequence-designer` | **Date**: 2026-05-16 | **Spec**: [spec.md](./spec.md)  
**Input**: Feature specification from `specs/005-sequence-designer/spec.md`

## Summary

Add a dedicated visual designer for `Elsa.Sequence` activities that uses the existing React Flow designer direction while enforcing Sequence semantics. The implementation should register a Sequence diagram designer provider, render children as a constrained ordered chain, derive visual edges from child order, persist changes back to the Sequence child activity collection, persist the selected orientation as Sequence designer metadata, and reuse existing activity selection, property editing, validation/status display, drill-in navigation, and designer shell behavior.

## Technical Context

**Language/Version**: C#/.NET 9-compatible Blazor modules; TypeScript 5.8 client bundle; React 18.3; `@xyflow/react` 12.3.5  
**Primary Dependencies**: `Elsa.Studio.Workflows`, `Elsa.Studio.Workflows.Core`, `Elsa.Studio.Workflows.Designer`, existing React Flow designer bundle, activity registry/name/identity services  
**Storage**: Workflow JSON activity model; Sequence child activity collection is source of truth; no new database storage  
**Testing**: `dotnet test` for .NET tests; `npm run build` in `src/modules/Elsa.Studio.Workflows.Designer/ClientLib`; manual Studio verification in a sample host  
**Target Platform**: Blazor Server and Blazor WebAssembly Studio hosts  
**Project Type**: Modular web application feature inside existing Studio workflows module  
**Performance Goals**: Sequence containing 100 child activities opens with readable layout and remains interactive for selection, pan/scroll, insert, and save operations  
**Constraints**: Do not persist arbitrary flowchart links for Sequence; disable manual connection creation in Sequence mode; do not require stored node positions for existing workflows; keep X6 path intact; preserve current Flowchart React Flow UX conventions  
**Scale/Scope**: One new Sequence designer mode/provider plus supporting mapper/interop updates; no backend API changes expected

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. Modular Studio Features**: Pass. Changes stay within workflows designer/workflows modules and use the existing diagram designer provider model.
- **II. Backend Capability Awareness**: Pass. No new backend capability is required; existing activity descriptors and workflow JSON are used.
- **III. UX Consistency and Density**: Pass. The design reuses the enhanced Flowchart visual language while constraining Sequence to ordered editing.
- **IV. Async, Disposal, and Real-Time Discipline**: Pass. React Flow JS resources must follow the existing `IAsyncDisposable` interop lifecycle.
- **V. Testing and Verification**: Pass. Plan includes mapper/unit tests, client bundle build, and manual large/empty/nested UI verification.
- **VI. Focused Change Sets**: Pass. Scope is limited to Sequence designer support and shared React Flow mode only where needed.
- **VII. Simplicity, DRY, and Maintainability**: Pass. Reuse current designer shell and React Flow components; introduce abstractions only for Sequence-specific ordered mapping.

## Project Structure

### Documentation (this feature)

```text
specs/005-sequence-designer/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── sequence-designer-contract.md
└── tasks.md
```

### Source Code (repository root)

```text
src/modules/Elsa.Studio.Workflows/
├── DiagramDesigners/
│   ├── Flowcharts/
│   └── Sequences/
├── Extensions/ServiceCollectionExtensions.cs
└── Shared/Components/DiagramDesignerWrapper.*

src/modules/Elsa.Studio.Workflows.Designer/
├── ClientLib/src/react-designer/
│   ├── components/
│   ├── internal/
│   └── types.ts
├── Components/
├── Contracts/
├── Interop/
└── Services/
```

**Structure Decision**: Implement the diagram provider and Blazor wrapper in `Elsa.Studio.Workflows` beside the Flowchart provider, and implement reusable React Flow rendering/interaction changes in `Elsa.Studio.Workflows.Designer`. This follows the existing split where workflow module owns activity-specific designers and the designer module owns shared canvas components.

## Phase 0: Research

See [research.md](./research.md).

## Phase 1: Design

See [data-model.md](./data-model.md), [contracts/sequence-designer-contract.md](./contracts/sequence-designer-contract.md), and [quickstart.md](./quickstart.md). The design supports vertical and horizontal Sequence layouts, with vertical as the default and orientation stored as non-execution designer metadata.

## Post-Design Constitution Check

- **I. Modular Studio Features**: Pass. The Sequence designer is added through `IDiagramDesignerProvider`; no module internals are crossed beyond existing public designer contracts.
- **II. Backend Capability Awareness**: Pass. No optional backend calls are introduced.
- **III. UX Consistency and Density**: Pass. Contract requires reuse of existing React Flow designer visual states and dense toolbox behavior.
- **IV. Async, Disposal, and Real-Time Discipline**: Pass. Contract requires disposal of React Flow graph resources through existing interop lifecycle.
- **V. Testing and Verification**: Pass. Quickstart includes empty, large, nested, read-only, and save/reload checks.
- **VI. Focused Change Sets**: Pass. Work can be sliced into provider/mapper/client-mode/test tasks.
- **VII. Simplicity, DRY, and Maintainability**: Pass. The data model keeps Sequence order authoritative and avoids a second persisted graph model.

## Complexity Tracking

No constitution violations require justification.
