# Build one BPMN modeler module for both Studios

Date: 2026-10-20

## Status

Proposed

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
load a document, save it, select an element, apply an overlay, and raise change and selection events.

- Documents go in and out as BPMN XML strings, the source of truth in
  [Keep BPMN XML as the source of truth for BPMN workflows](2026-10-20-bpmn-xml-as-source-of-truth.md). Load returns the
  import warnings bpmn-js reports.
- Selection, change and overlay calls identify elements by their BPMN element id, the same key the existing
  `BpmnElementStats` overlay and the execution-log diagnostics use.
- The exact payload shapes are fixed by the spike (#8901) and documented with the module.

- The Blazor Studio hosts it first, through JS interop, because it is the default Studio.
- The React Studio mounts the same module directly when its BPMN work reaches modeling.
- Studio-specific code is limited to hosting: dialogs, the activity picker and input editors, and the save flow.

## Consequences

- One modeler, one set of tests for modeling behavior, one `elsa:` moddle descriptor.
- The module's API is a compatibility surface between the two Studios and changes deliberately.
- The React Studio's BPMN work becomes hosting work, not modeler work.
- Blazor-only conveniences that bypass the module are not allowed, so the two Studios cannot drift.

## Alternatives considered

- **Build in the Blazor Studio only.** Faster to start, but the React Studio would need a second modeler later.
- **Build in the React Studio only.** The React Studio is a preview in 3.11 and not the default, so the modeler would
  not reach most users.
