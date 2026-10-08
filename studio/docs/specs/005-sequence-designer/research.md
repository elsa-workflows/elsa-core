# Research: Sequence Designer

## Decision: Use React Flow through the existing designer path

**Rationale**: The repository already includes `@xyflow/react`, React 18, `ReactFlowDesigner`, `ReactFlowGraphApi`, and a client bundle under `src/modules/Elsa.Studio.Workflows.Designer/ClientLib/src/react-designer`. The Flowchart wrapper can already route to React Flow via `DesignerOptions.UseReactFlow`. Reusing this path keeps Sequence visually consistent with the newer Flowchart UX and avoids building another canvas stack.

**Alternatives considered**:

- Extend the X6 designer. Rejected because the current UX direction is the React Flow designer and using X6 would make Sequence diverge from the improved Flowchart experience.
- Build Sequence as pure Blazor list UI. Rejected for MVP parity because the user explicitly wants the React Flow UX direction and shared activity nodes/viewport interactions.

## Decision: Model Sequence as a constrained ordered graph

**Rationale**: Sequence execution is defined by child order, not by arbitrary links. React Flow should render nodes and derived visual edges, while the saved model remains the ordered child activity collection.

**Alternatives considered**:

- Persist Sequence edges like Flowchart connections. Rejected because it changes Sequence semantics and creates invalid states.
- Render only a non-canvas list. Rejected because it loses the shared React Flow affordances already established by the Flowchart designer.

## Decision: Add a Sequence-specific mapper instead of overloading the Flowchart mapper

**Rationale**: `FlowchartMapper` maps activities plus explicit connections. Sequence needs mapping between an ordered activity collection and derived visual edges. A dedicated mapper keeps the rule explicit and avoids special cases inside Flowchart mapping.

**Alternatives considered**:

- Reuse `FlowchartMapper` with synthetic connections. Rejected because synthetic connections could accidentally become persisted or editable as real graph connections.
- Store position metadata for Sequence children. Rejected as required state because existing workflows should open without Sequence-specific layout metadata.

## Decision: Support vertical and horizontal layouts with persisted orientation

**Rationale**: Vertical layout is the clearest default for ordered execution and long Sequences. Horizontal layout is useful when users want a Flowchart-like scan direction or have wider canvas space. Persisting only orientation gives users control without making node positions part of Sequence execution semantics.

**Alternatives considered**:

- Vertical-only layout. Rejected because the user requested support for both directions.
- Persist full node positions. Rejected because Sequence should remain order-derived and existing workflows should not require layout metadata.

## Decision: Add a Sequence diagram designer provider beside the Flowchart provider

**Rationale**: `DefaultDiagramDesignerService` selects designers through `IDiagramDesignerProvider`. Adding `SequenceDiagramDesignerProvider` for `Elsa.Sequence` follows the existing extension pattern and keeps fallback designer behavior intact.

**Alternatives considered**:

- Special-case Sequence inside `DiagramDesignerWrapper`. Rejected because providers are the established extension point.
- Treat Sequence as a Flowchart. Rejected because arbitrary graph editing must remain disabled.

## Decision: Use drill-in affordances for embedded bodies and outcomes

**Rationale**: Existing designer navigation already supports embedded port selection and designer path changes. Sequence should surface embedded regions as previews or ports and delegate detailed editing to the existing drill-in flow.

**Alternatives considered**:

- Expand all nested content inline. Rejected because nested workflows become unreadable and expensive to render.
- Hide embedded regions until property editing. Rejected because authors need visual awareness of branches/bodies.

## Decision: Disable manual connection creation in Sequence mode

**Rationale**: Sequence edges are derived from order and must not imply graph authoring. Dedicated insert and reorder affordances keep authoring behavior explicit.

**Alternatives considered**:

- Convert manual connections into insert or reorder gestures. Rejected because it overloads Flowchart semantics and risks confusing users.
- Persist manual Sequence links. Rejected because it contradicts Sequence execution semantics.
