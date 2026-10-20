# Build the BPMN modeler on bpmn-js

Date: 2026-10-20

## Status

Accepted

## Context

Elsa 3 executes BPMN through `Elsa.Bpmn` and imports and exports it through `Elsa.Bpmn.Interchange`, but Studio cannot be
used to design a process. Studio's BPMN designer renders an imported document on an X6 canvas with every interaction
switched off (`graph-options.ts`): no palette, no connecting or moving shapes, no property editing. Visual editing was
planned as W14 (elsa-studio#1004) and deferred to v2 when #7909 closed.

A BPMN modeler is more than an editable canvas. It needs a palette, connection rules that only allow valid BPMN
(sequence flows inside a pool, message flows between pools, boundary events attached to activities), BPMN DI generation
for every shape and edge, label placement, expanded and collapsed subprocesses, snapping and alignment, undo and redo,
copy and paste, and a properties panel. Each of these is a separate piece of work on X6, which knows nothing about BPMN.

bpmn-js (bpmn-io/bpmn-js) is the modeler the BPMN ecosystem already uses, including Camunda Modeler. It provides the
canvas behavior above, reads and writes BPMN 2.0 XML directly, accepts vendor extensions through moddle descriptors, and
supports overlays for runtime state. The properties panel comes from two companion bpmn.io packages,
`bpmn-js-properties-panel` and `@bpmn-io/properties-panel`, which are extensible with custom providers. All of them are
framework-neutral JavaScript. The bpmn.io license permits commercial use. It requires the watermark that links to
https://bpmn.io to stay unchanged, fully visible, and not overlapped by other elements.

## Decision

Build Studio's BPMN modeler on bpmn-js and its properties-panel packages, not on X6.

- The `elsa:` vendor extension (`elsa:activityBinding`, `elsa:input`) is published as a bpmn-js moddle descriptor, so the
  modeler reads and writes bindings as BPMN rather than through a side channel.
- Elsa-specific editing, choosing the activity that performs a task and editing its inputs, is a properties-panel provider.
- Instance state (the per-element overlay Studio already computes from `BpmnElementStats`) is drawn with bpmn-js
  overlays and markers.
- The X6 BPMN renderer and its read-only adapter are retired once the modeler reaches parity with them.

A spike confirms the decision before Phase 3 of the BPMN v2 program starts. It checks:

- bundle size and load time in Blazor WebAssembly;
- the shared module's whole API through JS interop: load, save, selection, change events, overlays applied and cleared,
  and disposal;
- theming in light and dark;
- a lossless XML round trip through load, edit and save: the `elsa:` extensions, foreign extensions, unknown elements
  and attributes, subprocess bodies at every depth, and BPMN DI;
- editing `elsa:activityBinding` and `elsa:input` through the properties panel;
- a canvas layout in which no Studio element covers the bpmn.io watermark.

## Consequences

- Studio gains a standard BPMN editing experience without building one. Exported files are standard BPMN 2.0 XML with
  DI that other BPMN tools can open; those tools ignore the `elsa:` extensions, so the processes only run in Elsa.
- The X6 BPMN code from elsa-studio#1009, #1010 and #1024 is written off. Its view model and stats projection stay
  useful as inputs to overlays.
- The bpmn.io watermark is visible on the Studio canvas, and Studio's layout keeps that corner clear.
- Studio takes a dependency on bpmn-js and its release cadence; upgrades are tested against the `elsa:` descriptor.
- If the spike shows an unacceptable WebAssembly cost, this record is revisited before any modeler work is merged.

## Alternatives considered

- **Extend the X6 canvas (W14).** Keeps one canvas technology in Studio, but rebuilds the BPMN modeling rules,
  DI generation and properties panel that bpmn-js already provides. Rejected on cost.
- **Edit BPMN as XML or JSON in a form editor.** Cheap, but it does not meet the goal of designing a process visually.
