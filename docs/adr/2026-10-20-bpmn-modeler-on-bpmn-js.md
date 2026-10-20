# Build the BPMN modeler on bpmn-js

Date: 2026-10-20

## Status

Proposed

## Context

Elsa 3 executes BPMN through `Elsa.Bpmn` and imports and exports it through `Elsa.Bpmn.Interchange`, but Studio cannot be
used to design a process. Studio's BPMN designer renders an imported document on an X6 canvas with every interaction
switched off (`graph-options.ts`): no palette, no connecting or moving shapes, no property editing. Visual editing was
planned as W14 (elsa-studio#1004) and deferred to v2 when #7909 closed.

A BPMN modeler is more than an editable canvas. It needs a palette, connection rules that only allow valid BPMN
(sequence flows inside a pool, message flows between pools, boundary events attached to activities), BPMN DI generation
for every shape and edge, label placement, expanded and collapsed subprocesses, snapping and alignment, undo and redo,
copy and paste, and a properties panel. Each of these is a separate piece of work on X6, which knows nothing about BPMN.

bpmn-js (bpmn-io/bpmn-js) is the modeler the BPMN ecosystem already uses, including Camunda Modeler. It provides all of
the above, reads and writes BPMN 2.0 XML directly, accepts vendor extensions through moddle descriptors, supports
overlays for runtime state, and has an extensible properties panel. It is framework-neutral JavaScript. Its licence
permits commercial use and requires the bpmn.io logo to stay visible on the canvas.

## Decision

Build Studio's BPMN modeler on bpmn-js, not on X6.

- The `elsa:` vendor extension (`elsa:activityBinding`, `elsa:input`) is published as a bpmn-js moddle descriptor, so the
  modeler reads and writes bindings as BPMN rather than through a side channel.
- Elsa-specific editing, choosing the activity that performs a task and editing its inputs, is a properties-panel provider.
- Instance state (the per-element overlay Studio already computes from `BpmnElementStats`) is drawn with bpmn-js
  overlays and markers.
- The X6 BPMN renderer and its read-only adapter are retired once the modeler reaches parity with them.

A spike confirms the decision before Phase 3 of the BPMN v2 program starts: bundle size and load time in Blazor
WebAssembly, JS interop for load, save and selection, theming in light and dark, and the `elsa:` moddle round trip.

## Consequences

- Studio gains a standard BPMN editing experience without building one, and exported files match what other BPMN tools
  produce because the same library wrote them.
- The X6 BPMN code from elsa-studio#1009, #1010 and #1024 is written off. Its view model and stats projection stay
  useful as inputs to overlays.
- The bpmn.io logo is visible on the Studio canvas.
- Studio takes a dependency on bpmn-js and its release cadence; upgrades are tested against the `elsa:` descriptor.
- If the spike shows an unacceptable WebAssembly cost, this record is revisited before any modeler work is merged.

## Alternatives considered

- **Extend the X6 canvas (W14).** Keeps one canvas technology in Studio, but rebuilds the BPMN modeling rules,
  DI generation and properties panel that bpmn-js already provides. Rejected on cost.
- **Edit BPMN as XML or JSON in a form editor.** Cheap, but it does not meet the goal of designing a process visually.
