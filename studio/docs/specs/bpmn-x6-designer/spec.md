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
| Commands | Undoable edits. Part 1 has `updateProperties` and `setBinding`; step 3 adds `moveShapes`, `resizeShape` and `deleteElements`; step 4 adds `createShape` and `replaceElement`; later steps add commands that change semantics and layout together. |
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

`deleteElements(document, elementIds)` deletes flow elements, artifacts, message flows and data associations, and what
depends on them, as the bpmn-js designer does:

- what they contain, and the diagram of a deleted subprocess's own plane;
- the boundary events attached to anything deleted;
- the sequence flows, message flows, associations and data associations with a deleted end;
- a data object whose last reference is deleted;
- a group's category value when its last group is deleted, and a category when its last value is;
- every drawing of any of it.

Every reference the rest of the document holds to what is deleted is removed, such as a flow node's `incoming` and
`outgoing`, a gateway's `default` and a lane's `flowNodeRef`. Undo puts each element and reference back at its position.
It throws for an unknown element and for participants, lanes and processes, which step 6 deletes.

`createShape(document, planeId, type, position)` creates a start, intermediate throw or end event, an exclusive gateway, a
task, a data object reference, a data store reference, a group or a text annotation, with its shape centred on
`position` at the size the bpmn-js designer gives it:

- it goes into the innermost expanded subprocess or participant whose shape holds `position`, else into what the plane
  draws: its process, or the subprocess a collapsed subprocess's plane opens;
- a text annotation or group outside every participant of a collaboration goes into the collaboration;
- a flow node is also referenced by the innermost lane of its process that holds `position`;
- a data object reference gets a data object of its own;
- a group gets a category of its own in the definitions, with one value that holds the group's name.

Ids follow the bpmn-js designer: `Activity_`, `Event_`, `Gateway_` or the type's name, a random suffix, and `_di` added
for the shape. The command's `elementIds` start with the new element's id. Where the element goes is worked out when the
command executes. It throws for an unknown plane, a type it cannot create, a position that is not finite, and anything
but a text annotation or group outside every participant of a collaboration.

`replaceOptions(document, elementId)` lists what the bpmn-js designer's replace menu offers for an element, with its
labels, without what the element already is: other kinds of task, other gateways, and other events and event
definitions. A cancel end or boundary event is offered only in or on a transaction, and a subprocess's start event is
offered only as a plain event. `replaceElement(document, elementId, target)` replaces the element with one of the
target's type, as the bpmn-js designer does:

- it keeps the element's id, its place in its container and its shapes;
- it keeps every property both types have, such as its name, documentation, binding, flows, default flow and loop
  characteristics, and drops the rest, such as a script task's script;
- an event keeps its event definitions when they are of the target's kind, gets a new one otherwise, or none for a
  plain event; a boundary event's `cancelActivity` and a start event's `isInterrupting` come from the target;
- every reference to the element, such as a flow's source or target, a lane's `flowNodeRef` and a boundary event's
  `attachedToRef`, refers to the new one, and an exclusive gateway's shape shows its marker;
- a start event loses its incoming flows and an end event its outgoing flows.

It throws for an unknown element and a target not among its options. A create or a replace makes its objects once, so
a redo puts back the objects that the commands after it changed.

### Dirty tracking

Each executed command gets a new state id; undo and redo move along the same history. The document is clean exactly when
its current state is the one last saved. A save marks clean the state it wrote, captured before writing, so an edit made
while it runs stays dirty. A load clears the history and is clean.

## Limits

These are recorded here until step 9 writes the designer's limits document.

- Concurrent loads are not ordered: the load that finishes last wins. The designer serializes them.
- Element ids inside foreign extension content are not indexed. A rename can reuse one.
- A document with duplicate ids keeps the first element with each id in the index, as bpmn-moddle resolves references
  to it.

What the designer draws and edits, as of step 4c:

- Shapes can be moved by dragging, one at a time. A selected expanded subprocess, text annotation or group shows four
  corner handles that resize it; the resize stops at its minimum size and at what it contains. Delete or Backspace
  deletes the selected element. Participants and lanes are resized and deleted in step 6, with their lanes.
  Dragging snaps a shape's position to a 10-pixel grid. Edges are redrawn when the drag ends, not during it.
- An edit that removes the selected element clears the selection and raises `selection`.
- The palette creates the elements `createShape` creates, in the bpmn-js designer's palette order and with its names,
  and ends with a text annotation, which bpmn-js adds from its context pad. Drag an entry onto the canvas, or click it
  and then click the canvas. Escape gives a create up. The new element is selected,
  and a new task or text annotation opens for its name or text, as in the bpmn-js designer. The palette has no hand,
  lasso, space or connect tool, and no expanded subprocess or participant: subprocesses come in step 7 and participants
  in step 6. It cannot be used from the keyboard. It is disabled on the page that shows
  what an older tool drew for a collapsed subprocess on its parent's plane, until step 7, and absent when read-only.
- A create outside every pool of a collaboration is refused, and nothing changes.
- A selected task, gateway or event shows a wrench beside its top-right corner. The wrench, or R, opens the menu of
  what `replaceOptions` offers; arrow keys move through it, Enter or a click replaces, and Escape or a press elsewhere
  closes it. The menu opens below the wrench, or on the side with more room, shortened to fit, when it does not fit
  there. The element stays selected. Whenever an edit, undo or redo changes the selected element's type or name,
  `selection` is raised again. Subprocesses, call activities, event subprocesses and their start events, and data
  references are not replaced until step 7. Flows an event-based gateway cannot have are kept until step 5 adds
  connection rules.
- A shape dropped onto another subprocess, participant or lane stays in the one it was in.
- Lanes, labels and edges cannot be dragged. A boundary event moves only with its activity, until step 5 attaches and
  detaches them.
- A double-click on an element, or F2 or Enter on the selected one, edits its name in a text box over its label; a
  text annotation's text is edited the same way, and a group's name is its category value's `value`, as in the
  bpmn-js designer. A group without a category value has no name to edit. Enter or leaving the box saves it as one `updateProperties` edit, an
  empty name removing the name; Shift+Enter starts a new line; Escape gives up. A double-click on a collapsed
  subprocess opens its plane instead, so F2 edits its name. A redraw during an edit gives the edit up.
- A named edge without label layout is drawn with its name on its midpoint, as the bpmn-js designer places it. Naming
  an element does not add or resize its label layout.
- Ctrl+Z (Cmd+Z) undoes, and Ctrl+Shift+Z or Ctrl+Y redoes, while the canvas has focus.
- Pools and lanes are drawn horizontally, whatever their DI says.
- DI colours (`bioc:` and `color:` attributes) are kept in the document but not drawn.
- An exclusive gateway always shows its X; the DI's `isMarkerVisible` is not read.
- An element drawn twice on one plane is drawn once there, at its first DI.
- A document with several diagrams opens on the first one that is not a subprocess's plane; the others are reached only
  through `select`.

## Decisions

The owner answered the first three open questions on
[#8928](https://github.com/elsa-workflows/elsa-core/issues/8928#issuecomment-6384399178).

1. **The read-only view is retired** once the X6 designer ships, as plan step 10. It serves only workflow definitions
   without `Bpmn:SourceXml`, which no designer can open.
2. **BPMN properties beyond the binding are in scope.** These are conditions, timers, message references and
   `cancelActivity`. Plan step 8 adds a properties panel to the X6 designer for them, rendered in the contract's
   `propertiesContainer`. It is X6's own: the contract gains nothing, and the bpmn-js designer keeps its own panel.
3. **Pools with their own executable process are drawn as black boxes.** Elsa binds one process per definition, so
   the X6 designer edits the content of the pool whose process Elsa runs and draws every other pool as a collapsed
   participant. Step 6 implements this, and the limits document records it.

## Open questions for the owner

None of these block the next steps.

1. **Where the kernel lives.** It is in the shared `Elsa.Studio.Bpmn` ClientLib, as a separate entry, so both the X6
   designer and a future React Flow designer import it by path, as the bpmn-js designer imports the shared rules. If the
   React Studio cannot import by path, the kernel moves to a package of its own. Keep it here until then?
