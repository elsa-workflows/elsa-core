# Build the BPMN modeler on bpmn-js

Date: 2026-10-20

## Status

Accepted

Amended by [Offer selectable BPMN designers](2026-10-20-selectable-bpmn-designers.md): bpmn-js is the default BPMN
designer, and X6 and React Flow designers are offered beside it instead of the X6 view being retired.

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
- Elsa-specific editing, choosing the activity that performs a task and editing its inputs, uses Studio's activity
  picker and input editors through the designer contract's binding operations
  ([Build one BPMN modeler module for both Studios](2026-10-20-one-bpmn-modeler-for-both-studios.md)). The bpmn-js
  properties panel shows them through a provider that calls the same operations.
- Instance state (the per-element overlay Studio already computes from `BpmnElementStats`) is drawn with bpmn-js
  overlays and markers.
- bpmn-js is the default BPMN designer in both Studios. Other designers implement the same contract
  ([Offer selectable BPMN designers](2026-10-20-selectable-bpmn-designers.md)).
- The round trip preserves the `elsa:` extensions, foreign extension elements and attributes, subprocess bodies at
  every depth and BPMN DI. Content bpmn-js cannot represent (unknown elements outside `extensionElements`, a second
  element with the same id, XML comments, mixed text and element content) is reported by `load` and never dropped
  silently: saving after such a report needs the user's explicit acknowledgement.
- A document without BPMN DI is given generated DI (for example with `bpmn-auto-layout`) before it opens in the
  modeler.
- The module is loaded on demand, with a dynamic `import()` when a BPMN canvas opens, never when Studio starts. It is
  about 390 KiB brotli, more than half of it the properties-panel stack.
- In the dark theme the canvas stays light, so the watermark keeps its contrast without changing how it is drawn.
- No Studio control floats over the canvas's bottom-right corner, where the watermark sits.

A spike (#8901) checked the decision before Phase 3 of the BPMN v2 program, and the decision went ahead with the
conditions above. Its findings are in `studio/spikes/bpmn-js-modeler/FINDINGS.md` on the `spike/bpmn-js-modeler`
branch. It checked:

- bundle size and load time in Blazor WebAssembly;
- the shared module's whole API through JS interop: load, save, selection, change events, overlays and markers applied and cleared,
  and disposal;
- theming in light and dark;
- a lossless XML round trip through load, edit and save: the `elsa:` extensions, foreign extensions, unknown elements
  and attributes, subprocess bodies at every depth, and BPMN DI;
- editing `elsa:activityBinding` and `elsa:input` through the properties panel;
- a canvas layout in which no Studio element covers the bpmn.io watermark.

## Consequences

- Studio gains a standard BPMN editing experience without building one. Exported files are standard BPMN 2.0 XML with
  DI that other BPMN tools can open; those tools ignore the `elsa:` extensions, so the processes only run in Elsa.
- The X6 BPMN code from elsa-studio#1009, #1010 and #1024 stays as the starting point of the X6 designer. Its view
  model and stats projection also feed the bpmn-js overlays.
- The bpmn.io watermark is visible on the Studio canvas, and Studio's layout keeps that corner clear. In the dark theme
  the BPMN canvas stays light.
- Studio takes a dependency on bpmn-js and its release cadence; upgrades are tested against the `elsa:` descriptor.
- A save can lose content bpmn-js cannot represent, but only after the user has seen the list and agreed to it.

## Alternatives considered

- **Extend the X6 canvas (W14).** Keeps one canvas technology in Studio, but rebuilds the BPMN modeling rules,
  DI generation and properties panel that bpmn-js already provides. Rejected as the default on cost; offered as an
  alternative designer by [Offer selectable BPMN designers](2026-10-20-selectable-bpmn-designers.md).
- **Edit BPMN as XML or JSON in a form editor.** Cheap, but it does not meet the goal of designing a process visually.
