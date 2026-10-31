# Offer selectable BPMN designers

Date: 2026-10-20

## Status

Accepted

Amends [Build the BPMN modeler on bpmn-js](2026-10-20-bpmn-modeler-on-bpmn-js.md) and
[Build one BPMN modeler module for both Studios](2026-10-20-one-bpmn-modeler-for-both-studios.md).

## Context

The two records above choose bpmn-js as the BPMN modeler, share it between the Blazor and React Studios, and retire the
X6 BPMN renderer once the modeler reaches parity. The bpmn-js spike (#8901) confirmed that bpmn-js works in both
Studios, under conditions now recorded in those two records.

Each Studio already has its own canvas technology. The Blazor Studio draws flowcharts and today's read-only BPMN view
with X6. The React Studio (#8859) is built on React Flow. Some teams embedding Studio want BPMN on the same canvas as
the rest of their designer, without bpmn-js's bundle (about 390 KiB brotli) or its watermark. Others want the standard
BPMN modeling experience bpmn-js gives with no further work.

## Decision

A BPMN designer is a replaceable Studio component. Studio ships three of them, and the host chooses which one it
enables.

| Designer | Studio | Role |
| --- | --- | --- |
| bpmn-js | Blazor and React | The default in both Studios. |
| X6 | Blazor | An alternative to bpmn-js in the Blazor Studio. |
| React Flow | React | An alternative to bpmn-js in the React Studio. |

- **One contract.** Every designer implements the designer API defined in
  [Build one BPMN modeler module for both Studios](2026-10-20-one-bpmn-modeler-for-both-studios.md): load a BPMN XML
  document and report what it could not represent, save it, select an element, read and replace an element's `elsa:`
  binding, apply and clear overlays keyed by BPMN element id, raise change and selection events, and dispose. Studio
  code outside the designer depends only on that contract: the activity picker and input editors follow the selection
  and edit through the binding operations, so they, the save flow and the instance overlays work with any designer.
  Version 1 of the contract is specified in `studio/docs/specs/bpmn-designer-contract/contract.md`.
- **One module per designer.** Each designer is its own Studio module, such as `Elsa.Studio.Bpmn.BpmnJs` and
  `Elsa.Studio.Bpmn.X6` in the Blazor Studio. A host enables a designer the way it enables any other Studio feature.
  When a host enables none, the Studio's BPMN feature enables bpmn-js. A designer the host does not enable adds nothing
  to its download.
- **Shared rules, not shared drawing.** What does not depend on the canvas is written once and used by every designer:
  - the `elsa:` binding model;
  - generating BPMN DI for documents that have none;
  - the rule that content a designer cannot represent is reported by `load`, and a save after such a report needs the
    user's explicit acknowledgement;
  - validation through the server's analyze endpoint.
- **One conformance suite.** Every designer passes the same tests before it ships:
  - the round-trip preservation set: `elsa:` extensions, foreign extension elements and attributes, subprocess bodies
    at every depth, and BPMN DI;
  - the load-warning contract;
  - Elsa importing the saved document identically;
  - the binding operations: `getBinding` and `setBinding` read and write the binding, refuse elements other than tasks,
    keep the element's other extension content, undo in one step and raise `change`;
  - overlays and events.
- **Phased delivery.** bpmn-js ships first, in the Blazor Studio and then the React Studio. The X6 and React Flow
  designers follow as later phases and become full editors: palette, BPMN connection rules, DI for new shapes,
  subprocess editing and properties.

The existing read-only X6 BPMN view stays until the X6 designer replaces it. It is no longer retired in favor of bpmn-js.

## Consequences

- Teams choose between a standard BPMN modeling experience and a designer native to their Studio's canvas.
- Studio maintains three BPMN designers. bpmn-js provides its modeling rules; the X6 and React Flow designers have to
  implement theirs: BPMN connection rules, DI generation for new shapes and edges, label placement, collapsed and
  expanded subprocesses, and undo and redo. This is the cost the earlier record rejected for X6, now accepted to give
  developers the choice, and the main reason they come after bpmn-js.
- The designer contract and the conformance suite are compatibility surfaces. Changing either means changing every
  designer.
- A document saved by one designer must open in the others. The conformance suite is what keeps that true.
- The support matrix ([Publish the supported BPMN subset as a tested matrix](2026-10-20-published-bpmn-support-matrix.md)) describes
  runtime behavior and does not depend on the designer. Designer-specific limits are documented per designer.

## Alternatives considered

- **bpmn-js only, retire X6.** The earlier decision. One designer is cheapest, but it forces every host onto bpmn-js's
  bundle and watermark, and it leaves the React Studio's own canvas without BPMN.
- **One native designer per Studio, no bpmn-js.** Avoids the bpmn-js dependency, but rebuilds the whole modeler twice
  before anyone can model BPMN.
- **Choose the designer at run time.** Every designer would ship to every host, so the bundle grows by all of them. A
  Studio module keeps the cost to what the host enables.
