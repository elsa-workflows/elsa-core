# X6 BPMN designer

An editing BPMN designer for the Blazor Studio built on AntV X6, selectable beside the bpmn-js designer
([#8928](https://github.com/elsa-workflows/elsa-core/issues/8928)). It implements the
[BPMN designer contract](../bpmn-designer-contract/contract.md), version 1, so the code around a designer (the activity
picker, the input editors, the save flow, the instance overlays) works with it unchanged.

The decisions behind selectable designers are in the ADR "Offer selectable BPMN designers"
(`docs/adr/2026-10-20-selectable-bpmn-designers.md`). The steps that build this designer are in [plan.md](plan.md).

## Done when

As the issue states it:

- It passes the shared designer conformance suite (`studio/test/browser/BpmnDesigner`).
- Its designer-specific limits are documented.

What the issue asks of the editor: a palette; BPMN connection rules (sequence flows inside a pool, message flows between
pools, boundary events attached to activities); layout (DI) for new shapes and edges, and label placement; expanded and
collapsed subprocesses, editable at every depth; undo and redo; properties through Studio's activity picker and input
editors. The plan adds two checks: the Elsa import equivalence check runs against the X6 designer in CI, and a document
it saves opens in the bpmn-js designer with no warnings.

## Architecture

### The bpmn-moddle tree is the document; X6 only draws it

The designer holds a document as the bpmn-moddle object tree the bpmn-js designer holds, read and written by the same
moddle with Elsa's `elsa:` extension registered. X6's graph is a projection of that tree, rebuilt or patched from it,
and never the source of truth.

- **Load** runs `withGeneratedLayout`, then `fromXML`. The document is replaced only when both succeed, so a load that
  fails keeps the document, its edits and its undo history.
- **Save** runs `toXML(definitions, { format: true })`, the serializer and options bpmn-js uses. Content an edit does
  not touch is written byte for byte as bpmn-js writes it, and a saved document saves again unchanged.
- **Load warnings** come from the shared `computeLoadWarnings`, which compares the original with what a save right after
  loading writes. Foreign extension elements and attributes are kept as bpmn-moddle keeps them.

The alternative, X6's graph JSON as the model with a hand-written XML writer, is rejected. It would have to rebuild
every preservation rule bpmn-moddle already implements (child order, namespaces, foreign attributes, planes), and its
output would drift from what bpmn-js writes.

### A canvas-neutral modeling kernel

The document and every edit to it live in a kernel with no canvas:
`studio/src/modules/Elsa.Studio.Bpmn/ClientLib/src/modeling/`, in the shared ClientLib beside the contract and the
binding rules. A boundary test keeps X6, React, bpmn-js and diagram-js out of it, directly or through what it imports,
so the React Flow designer ([#8929](https://github.com/elsa-workflows/elsa-core/issues/8929)) can build on it too.

It is a separate entry from the shared ClientLib's `src/index.ts`, so the bpmn-js designer's bundle does not carry it.

| Part | What it does |
| --- | --- |
| `BpmnDocument` | Reads and writes a document. Indexes its semantic elements by id, and its diagram planes. |
| `planeView(planeId)` | What one plane draws, in a canvas's terms: shapes with bounds, labels, containers and attachments; edges with waypoints and ends. |
| `CommandStack` | Undo, redo and dirty tracking by state id, ported from the bpmn-js designer. |
| Commands | Undoable edits. Part 1 has `updateProperties` and `setBinding`; step 3 adds `moveShapes` and `resizeShape`; later steps add commands that change semantics and layout together. |
| `BpmnModelingSession` | The contract's canvas-independent half: `load`, `save`, `getBinding`, `setBinding`, `undo`, `redo` and the change event. |

Undo and redo live in the kernel, not in X6's History plugin, because the X6 graph is only a projection: undoing a
graph change would not undo the document.

Later steps add to the kernel an element factory, connection, drop and attach rules, orthogonal edge layout and label
placement, all canvas-neutral.

### The `Elsa.Studio.Bpmn.X6` module

The module follows the bpmn-js module's layout: a designer descriptor with the id `x6`, an `AddX6BpmnDesigner()`
extension, and an esbuild ClientLib with exact dependency pins and a bundle size budget. The "Performed by" panel
already works through `getBinding` and `setBinding` for any designer.

## How the kernel behaves

### Reading

`BpmnDocument.read(xml)` resolves to the document and its load warnings, or rejects when `xml` is not XML or its root is
not `bpmn:definitions`.

Reading changes nothing in the tree. bpmn-js does: it moves the content of a collapsed subprocess drawn on its parent's
plane, as older tools and bpmn-auto-layout write it, to a plane of its own, and it writes each plane's shapes in drawing
order. The kernel keeps both as the document has them. The semantic part of a save is the same as bpmn-js writes; only
the order and placement of diagram elements differ. The load warnings are the same for every fixture and Elsa test
asset.

### Planes

`planes()` lists every `BPMNPlane` in document order, with the element it draws. A collapsed subprocess's own plane is a
plane like any other, drawing the subprocess. `planeOfElement(id)` is the plane that first draws an element.

`planeView(planeId)` lists every shape and edge of the plane exactly once, in document order, except one whose element
the document does not declare. A shape says:

- `isExpanded` for a subprocess, call activity or participant, decided as bpmn-js decides it: a subprocess is expanded
  only when its shape says so, a participant when it has a process.
- `parentId`: the shape on the plane that contains it, its expanded or collapsed subprocess, else the participant whose
  process holds it.
- `hiddenBy`: the outermost collapsed subprocess on the plane that contains it. Only content an older tool drew on the
  collapsed subprocess's parent plane has it. A designer draws such content on a plane of its own, as bpmn-js does,
  without moving it in the document.
- `attachedToId` for a boundary event.

An edge gives its waypoints and the elements it leaves and enters. A data association runs between its data and the
activity that owns it, as bpmn-js draws it.

### Editing

A command is created against the document and changes nothing until the session executes it. Each command undoes
exactly what it did, including removing a property or foreign attribute it added. A compound command undoes as one step.

`updateProperties(document, elementId, properties)` sets properties, removes those set to `undefined`, and sets a
namespaced attribute the schema does not declare, such as `vendor:origin`, as that attribute. An id change reindexes the
document; references follow, because they are objects in the tree. It throws for an unknown element and for an id the
document already uses, a diagram element's included.

`setBinding` follows the bpmn-js designer exactly: an existing binding is replaced where it stands, an emptied
`extensionElements` is removed unless it carries foreign attributes, and removing a binding an element does not have
is applied with no edit.

`moveShapes(document, planeId, elementIds, delta)` changes layout only. These move by `delta`, with their labels:

- the shapes of the elements given;
- the shapes those contain, at any depth;
- the boundary events attached to any of them.

Edges follow:

- An edge between two moved shapes moves as it is.
- An edge with one moved end keeps its other end. The segment at the moved end stays horizontal or vertical if it
  was: the bend after it follows on one axis, and a straight two-point edge gets two bend points halfway along.
- An edge's label keeps its offset from the edge's midpoint.

Positions come from the layout when the command executes, so two moves in one compound add up. A move never changes
which subprocess, participant or lane an element belongs to. It throws for:

- a lane, which moves with its participant;
- a boundary event without its activity;
- an element with no shape on the plane;
- an unknown plane;
- a delta that is not finite.

`resizeShape(document, planeId, elementId, bounds)` gives a shape new bounds. Following the bpmn-js designer, it resizes
only an expanded subprocess (minimum 140 by 120), a text annotation (50 by 30) and a group (50 by 50); `minimumSize`
says which.

- A boundary event attached to the shape stays on its border, keeping its distance from the nearer side on each axis.
- An edge end on the border moves the same way, and the edge stays orthogonal as a move keeps it.
- An edge end on one of those boundary events moves with the event.
- What an expanded subprocess contains stays where it is, and the new bounds must still contain it.

It throws for a shape it cannot resize, bounds below the minimum or not finite, and bounds that leave out the content.

### Dirty tracking

Each executed command gets a new state id; undo and redo move along the same history. The document is clean exactly when
its current state is the one last saved. A save marks clean the state it wrote, captured before writing, so an edit made
while it runs stays dirty. A load clears the history and is clean.

## Limits

These are recorded here until step 8 writes the designer's limits document.

- Concurrent loads are not ordered: the load that finishes last wins. The designer serializes them.
- Element ids inside foreign extension content are not indexed. A rename can reuse one.
- A document with duplicate ids keeps the first element with each id in the index, as bpmn-moddle resolves references
  to it.

What the designer draws and edits, as of step 3b:

- Shapes can be moved by dragging, one at a time. A selected expanded subprocess, text annotation or group shows four
  corner handles that resize it; the resize stops at its minimum size and at what it contains. Nothing can yet be
  created or deleted (steps 3c to 7). Participants and lanes are resized in step 6, with their lanes.
  Dragging snaps a shape's position to a 10-pixel grid. Edges are redrawn when the drag ends, not during it.
- A shape dropped onto another subprocess, participant or lane stays in the one it was in.
- Lanes, labels and edges cannot be dragged. A boundary event moves only with its activity, until step 5 attaches and
  detaches them.
- Ctrl+Z (Cmd+Z) undoes, and Ctrl+Shift+Z or Ctrl+Y redoes, while the canvas has focus.
- Pools and lanes are drawn horizontally, whatever their DI says.
- DI colours (`bioc:` and `color:` attributes) are kept in the document but not drawn.
- An exclusive gateway always shows its X; the DI's `isMarkerVisible` is not read.
- An element drawn twice on one plane is drawn once there, at its first DI.
- A document with several diagrams opens on the first one that is not a subprocess's plane; the others are reached only
  through `select`.

## Open questions for the owner

None of these block steps 1 to 3.

1. **Retiring the read-only view.** The read-only BPMN view now serves only workflow definitions without
   `Bpmn:SourceXml`, which no designer can open. Retire it once the X6 designer ships (step 9), or keep it for those
   definitions?
2. **BPMN properties beyond the binding**, such as conditions, timers, message references and `cancelActivity`. Neither
   designer can edit them in the Blazor Studio today. Recommendation: an optional contract addition, in a separate
   issue, and a documented limit for this one.
3. **Pools with their own executable process.** Elsa binds one process per definition. Recommendation: draw other pools
   as black boxes, and document the limit.
4. **Where the kernel lives.** It is in the shared `Elsa.Studio.Bpmn` ClientLib, as a separate entry, so both the X6
   designer and a future React Flow designer import it by path, as the bpmn-js designer imports the shared rules. If the
   React Studio cannot import by path, the kernel moves to a package of its own. Keep it here until then?
