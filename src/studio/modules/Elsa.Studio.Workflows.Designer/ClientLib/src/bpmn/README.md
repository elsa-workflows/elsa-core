# `src/bpmn` — the canvas-neutral BPMN view model

Everything that is true about a BPMN diagram regardless of who draws it.

The design (elsa-workflows/elsa-core#7909 §4, D2) puts most of Studio's BPMN logic here on purpose,
so that the X6 adapter and a later React Flow adapter stay thin. The review test for anything added
here or to an adapter: **if it would be identical in both adapters, it belongs in this module.**

Pure TypeScript. Nothing here may import `@antv/x6`, `@xyflow/react`, `react`, or anything from
`../designer` or `../react-designer`; `__tests__/module-boundaries.test.ts` enforces it.

## The two inputs

BPMN DI is **not** on the activity payload. In `Bpmn.Model`'s schema,
`bpmnDiagram` / `bpmnPlane` / `bpmnShape` / `bpmnEdge` / `bpmnBounds` / `bpmnLabel` hang off
`bpmnDefinitions.diagrams`, while the `Elsa.BpmnProcess` activity's `process` property is a
`bpmnProcessDefinition` — `processId, name, isExecutable, isTransaction, elements, sequenceFlows,
lanes, variables, extensions` — with no diagram section at all. Pools (`participant`) live on
`bpmnDefinitions.collaboration`, likewise absent. So structure and geometry come from two different
places, and `buildBpmnViewModel` takes both:

1. **The `Elsa.BpmnProcess` activity JSON** (`process`, `workBindings`, `activities`, and the nested
   `BpmnProcess` activities that are the subprocess bodies), typed by `types.generated.ts`. This is
   the source of truth for **structure and bindings**.
2. **The source BPMN document**, optional, as a string. elsa-core stores it on the workflow
   definition's `CustomProperties["Bpmn:SourceXml"]` (with `Bpmn:SourceVersion`); Studio's
   `WorkflowDefinition` client model exposes `CustomProperties`, and the designer receives the
   definition through `DisplayContext.WorkflowDefinition`. W10 passes both across JS interop.

`di-reader.ts` reads **only** the BPMNDI section of that document — `BPMNShape[@bpmnElement,
dc:Bounds, isExpanded, isHorizontal, isMarkerVisible, BPMNLabel/dc:Bounds]`, `BPMNEdge[@bpmnElement,
di:waypoint*, BPMNLabel]` — plus the participant names on `collaboration` that a pool needs to
render. It never reads elements, flows, lanes or bindings. That boundary is the point: it keeps this
module from becoming a second BPMN reader that can disagree with the first one.

DI shapes and edges are matched to payload elements and flows **by id**.

## When the document carries no geometry

A `.bpmn` file is allowed to have no BPMNDI section, and elsa-core's own test assets include two that
do not. That is the one case that needs a layout of Studio's own, and it is **reported, not silently
invented**:

* `layout.source` reads `'fallback'` and `layout.reason` says why (no source XML / no diagram
  section / unparseable / a diagram with no shape for anything here);
* a `missing-source-xml`, `missing-diagram` or `source-xml-parse-error` diagnostic is raised;
* every element placed by the fallback reports `geometry.source: 'fallback'`.

`fallback-layout.ts` is a deterministic left-to-right layering by sequence flow order, recursing into
subprocess bodies. Where a document has *partial* DI — a diagram, but no shape for some element —
`layout.source` stays `'document'`, the unplaced elements get a fallback rectangle each, and each one
raises its own `missing-shape` diagnostic. Check `geometry.source` per element to tell them apart.

## What is out of scope, by design

* **No BPMN semantics.** Which flow a gateway takes, whether a join may fire, what a boundary event
  interrupts: none of that is here. This module renders what the document says and what the instance
  overlay reports. `BpmnBoundaryAttachment.interrupting` is the document's `cancelActivity` flag and
  nothing more.
* **No save path.** D4: the client holds the *entire* document and never writes a reduced projection
  back. This module reads. Whatever mutates the document (W11, W14) mutates it in place and sends
  the whole thing.
* **No auto-layout of a document that has DI.**

## Instance state

Two maps, both optional, both resolved onto elements:

* `elementStats`, keyed by **BPMN element id** — the projection W13 fills in. This is the one that
  lets a gateway or an event light up: they are not bound work, so they have no activity id.
* `activityStats`, keyed by **Elsa activity id** — the existing
  `Elsa.Studio.Workflows.Domain.Models.ActivityStats`, resolved onto elements through `workBindings`.
  Only bound work has one.

An element carries both, as `stats` and `activityStats`.

## Files

| File | What it does |
| --- | --- |
| `index.ts` | The public surface. Adapters and interop import from here. |
| `model.ts` | Every hand-authored input and output type. Plain immutable data, no methods. |
| `view-model.ts` | `buildBpmnViewModel`. Walks the scope tree, resolves bindings, geometry and stats, collects diagnostics. |
| `di-reader.ts` | BPMN DI + collaboration participants, out of the source XML, via the platform `DOMParser`. |
| `fallback-layout.ts` | The deterministic layered layout for documents with no DI. |
| `element-kinds.ts` | Classification of the payload's `elementType` strings, mirroring `Bpmn.Model.BpmnElementTypes`. |
| `types.generated.ts` | Generated from `Bpmn.Model`'s published schema. Never edit by hand; see `../../scripts/generate-bpmn-types.js`. |
| `__fixtures__/` | `.bpmn` + captured `.activity.json` pairs. See its own README. |
