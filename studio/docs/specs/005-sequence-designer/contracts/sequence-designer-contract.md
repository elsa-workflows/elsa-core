# Sequence Designer Contract

## Provider Contract

The workflows module registers a Sequence provider through the existing diagram designer provider extension.

```text
Provider supports activity when activity.type == "Elsa.Sequence"
Provider priority must be high enough to beat fallback designer
Provider returns a Sequence diagram designer implementing IDiagramDesigner
```

## Blazor Designer Contract

The Sequence diagram designer must satisfy the same wrapper-level operations as other designers:

```text
LoadRootActivityAsync(sequence, activityStats)
UpdateActivityAsync(activityId, activity)
UpdateActivityStatsAsync(activityId, stats)
SelectActivityAsync(activityId)
ReadRootActivityAsync() -> updated sequence
DisplayDesigner(displayContext) -> render fragment
```

Expected callbacks:

```text
ActivitySelected(activity)
ActivityUpdated(activity)
ActivityEmbeddedPortSelected(activity, portName)
ActivityDoubleClick(activity)
GraphUpdated()
```

## Mapping Contract

Input:

```text
Sequence activity JSON
Optional activity stats map
```

Output for React Flow rendering:

```text
nodes: one node per direct Sequence child
edges: derived structural edge between each adjacent child pair
metadata: activity data, ports, display settings, stats, validation/status state
layoutOrientation: "vertical" | "horizontal" (defaults to "vertical")
mode: "sequence"
```

Readback:

```text
Read React Flow nodes in visual order
Update Sequence child collection to match that order
Discard derived structural edges
Persist selected layout orientation in Sequence designer metadata
Preserve child activity JSON and nested bodies
Return updated Sequence activity JSON
```

## Interaction Contract

Allowed interactions:

- Select child activity.
- Add first activity.
- Insert before, after, or between children.
- Reorder children using drag reorder.
- Reorder children using explicit move commands.
- Delete child.
- Duplicate/copy/paste child activities.
- Open embedded region through existing embedded-port callback.
- Pan, zoom, center, and zoom-to-fit.

Constrained interactions:

- Manual arbitrary connections are disabled.
- Structural Sequence edges are not selectable as persisted workflow connections.
- Auto-layout arranges by Sequence order and selected orientation.

Read-only mode:

- Selection, pan, zoom, center, drill-in, and status inspection are allowed.
- Add, insert, reorder, delete, duplicate, paste, and edit operations are disabled.

## Client Mode Contract

React Flow must be able to distinguish free-form Flowchart rendering from constrained Sequence rendering.

```text
mode = "flowchart" | "sequence"
```

Sequence mode requirements:

- Render derived vertical or ordered edges.
- Support vertical and horizontal orientations, with vertical as default.
- Hide or disable arbitrary connection creation.
- Emit graph updates when child order changes.
- Emit graph updates when layout orientation changes.
- Provide insertion affordances for empty and between-child positions.
- Provide explicit move commands for selected Sequence children.
- Preserve existing activity node visual states.
