# Build one BPMN modeler module for both Studios

Date: 2026-10-20

## Status

Accepted

Amended by [Offer selectable BPMN designers](2026-10-20-selectable-bpmn-designers.md): the module's API is the contract
every BPMN designer implements, and the X6 and React Flow designers implement it beside this module.

## Context

Elsa 3 now has two Studios. The Blazor Studio is the default. The React Studio is folded into `studio/react/` and ships
in 3.11 as an opt-in preview (#8859); its BPMN import and export parity is tracked in #8853. The Blazor Studio
also has an optional React Flow canvas (`DesignerOptions.UseReactFlow`), and its BPMN designer is not available there
either (W9b, elsa-studio#1003): BPMN already lives in one canvas per Studio, and only in one of them.

A BPMN modeler is the largest single piece of the BPMN v2 program. Building it twice, once per Studio, would double the
largest cost in the program and leave two modelers to keep consistent. bpmn-js (see
[Build the BPMN modeler on bpmn-js](2026-10-20-bpmn-modeler-on-bpmn-js.md)) is framework-neutral, so neither Studio needs
its own.

## Decision

Build the BPMN modeler once, as a framework-neutral JavaScript module around bpmn-js with a small, documented API:
load a document, save it, select an element, apply and clear overlays, raise change and selection events, and dispose
of the modeler. Each host calls dispose when it unmounts the canvas, which releases the bpmn-js instance and its event
handlers.

- Documents go in and out as BPMN XML strings, the source of truth in
  [Keep BPMN XML as the source of truth for BPMN workflows](2026-10-20-bpmn-xml-as-source-of-truth.md). Load returns the
  import warnings bpmn-js reports. When a warning reports content that a save would drop, the host saves only after the
  user has seen the list and explicitly acknowledged it.
- Selection, change and overlay calls identify elements by their BPMN element id, the same key the existing
  `BpmnElementStats` overlay and the execution-log diagnostics use.
- The payload shapes start from the ones the spike (#8901) used, and are documented with the module:
  - `load` returns `{ imported, error?, warnings: [{ message, elementId? }] }`;
  - `change` sends `{ elementIds, isDirty, canUndo, canRedo }`;
  - `selection` sends `{ elementIds, primary: { id, type, name? } | null }`;
  - `applyOverlays` takes the `BpmnElementStats` keyed by element id and returns `{ applied, unknownElementIds }`.
- This API is the BPMN designer contract. The X6 and React Flow designers implement it too, so the code around a
  designer never depends on which one is enabled.

- The Blazor Studio hosts it first, through JS interop, because it is the default Studio.
- The React Studio mounts the same module directly when its BPMN work reaches modeling.
- Studio-specific code is limited to hosting: dialogs, the activity picker and input editors, and the save flow.

## Consequences

- One modeler, one set of tests for modeling behavior, one `elsa:` moddle descriptor.
- The module's API is a compatibility surface between the two Studios and every BPMN designer, and changes
  deliberately.
- The React Studio's BPMN work becomes hosting work, not modeler work.
- Studio code outside a designer uses only the contract, so neither the two Studios nor the designers can drift.

## Alternatives considered

- **Build in the Blazor Studio only.** Faster to start, but the React Studio would need a second modeler later.
- **Build in the React Studio only.** The React Studio is a preview in 3.11 and not the default, so the modeler would
  not reach most users.
