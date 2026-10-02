# State Machine X6 Migration Contract

## Status

Accepted for implementation on 2026-08-03. This document extends the State
Machine designer specification without changing the Elsa runtime activity
contract.

## Ownership

- `StateMachineGraph`, `StateMachineMapper`, and `StateMachineValidator` remain
  the authoritative representation of State Machine semantics.
- A .NET editor session owns commands, validation, selection identity, and the
  decision to publish a graph update.
- X6 owns visual cells, routing, selection chrome, ports, pan, zoom, and layout
  gestures. JavaScript reports user intent; it does not author runtime State
  Machine JSON.
- Blazor owns the summary, Outline, validation feedback, state and transition
  inspectors, and embedded activity or expression editing.

## X6 Mode Seam

The shared X6 graph kernel supports three explicit modes:

- `flowchart`
- `sequence`
- `stateMachine`

Each mode declares its supported node and edge shapes, connection policy,
interaction policy, sizing behavior, and persistence filtering. State Machine
cells must never be treated as Elsa activities or flowchart connections.

## Canvas Projection

The renderer-neutral State Machine canvas projection contains stable visual
IDs, semantic indexes, state flags, transition endpoints, slot-presence flags,
validation state, and layout geometry. Semantic collection order is preserved
independently from visual coordinates.

State and transition names are mutable semantic values and therefore are not
used as X6 cell IDs. Duplicate or otherwise invalid domain items still receive
distinct session visual IDs so they remain visible and repairable.

## Interaction Contract

- Selecting a state or transition synchronizes Diagram, Outline, validation,
  and inspector selection.
- Moving a state changes layout only; attached edges follow continuously.
- Connecting or reconnecting an edge sends an intent to .NET. The editor
  session applies defaults, mutates the domain graph, validates it, and returns
  the accepted projection.
- Every pointer mutation has a Blazor form or Outline equivalent.
- Read-only mode retains selection, inspection, pan, zoom, fit, and center, but
  does not expose mutation handles.
- Invalid transitions are displayed and are never silently filtered from the
  domain graph.

## Layout Persistence

Layout metadata is Studio-owned and must not change State Machine execution
semantics or state/transition order. Before persistent visual IDs or geometry
are written, the workflow-definition backend must be proven to round-trip the
chosen `customProperties` metadata location. Until that contract is proven,
layout identity and positions remain session-only and deterministic layout is
used after reload.

Browser-local storage is not an acceptable silent fallback because layout must
travel with the workflow definition.

## Accessibility

The Blazor Outline remains a first-class synchronized view and supplies a
keyboard-complete alternative to graph gestures. Canvas nodes have accessible
names containing their state name and initial/current/terminal/validation
status. Focus, selection, runtime state, and validation state must remain
visually distinct and must not rely on color alone.

## Regression Gates

- Flowchart and Sequence graph reading must continue to preserve only their
  supported cells.
- Theme switching must redraw the grid and update State Machine cells without
  rebuilding the graph or losing selection.
- State and transition order and all unknown JSON properties must survive
  load, visual interaction, export, save, and reload.
- Browser tests cover node dragging with live edges, connect/reconnect,
  self-loops, parallel transitions, invalid endpoints, read-only behavior, and
  light/dark mode.
