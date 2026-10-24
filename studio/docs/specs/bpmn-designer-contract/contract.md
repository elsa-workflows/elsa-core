# BPMN designer contract, version 1

Every BPMN designer in Elsa Studio implements this contract, so the code around a designer does not depend on which
designer the host enabled. That code includes the activity picker, the input editors, the save flow and the instance
overlays.

The decisions behind it are in the ADRs "Build one BPMN modeler module for both Studios"
(`docs/adr/2026-10-20-one-bpmn-modeler-for-both-studios.md`) and "Offer selectable BPMN designers"
(`docs/adr/2026-10-20-selectable-bpmn-designers.md`). This document is the contract's exact shape. The TypeScript
types are in `studio/src/modules/Elsa.Studio.Bpmn/ClientLib/src/contract.ts`, and they and this document change
together.

## Module

A designer is a JavaScript module whose default export is a `BpmnDesignerModule`:

| Member | Meaning |
| --- | --- |
| `designerId` | A stable id, such as `bpmn-js` or `x6`. Hosts select a designer by this id. |
| `contractVersion` | The contract version the module implements. This document is version `1`. |
| `createDesigner(container, options)` | Creates a designer that draws into `container` and returns a `BpmnDesigner`. |

`options`:

- **`readOnly`:** when `true`, nothing the user does changes the document, and `setBinding` refuses with
  `read-only`. `load`, `save`, `select` and the overlays still work.
- **`propertiesContainer`:** an element where the designer may render its own properties panel. A designer may
  ignore it.

A host refuses a module whose `contractVersion` it does not support, instead of calling it.

## Designer

Every element is identified by its BPMN element id. After `dispose`, every method except `dispose` throws.

### `load(xml)`

Replaces the canvas content with the document. It returns `{ imported, error?, warnings }`:

- `imported` is `false` when the document could not be shown at all; `error` then says why. The designer keeps the
  document it had, and whether it was dirty, so `save` still returns that document. Its undo history and overlays may
  be cleared; when the undo state changes, `change` is raised. A designer that had no document still has none, and
  `save` throws until a load succeeds.
- `warnings` lists what the designer cannot represent or will change. Each warning is
  `{ kind, message, elementId? }`:
  - **`dropped`:** content that a save would lose.
  - **`rewritten`:** content that a save would keep, but write differently. For example, the diagram layout of a
    collapsed subprocess moved to its own diagram plane, with its geometry unchanged.

A designer computes warnings by comparing the original input with what a save right after loading would produce.
It does not rely on its parser's own messages alone, because content a parser accepts can still be lost on save.
`computeLoadWarnings(original, saved)` in the shared ClientLib does this comparison, by these rules:

- Elements and attributes are compared by namespace and local name. Prefixes, attribute order and whitespace-only
  text do not matter, and neither does content the save adds.
- An element is matched by its `id`, else its `name`, else its position among siblings of the same kind. Of two
  elements that share an `id`, the one a save does not keep is `dropped`.
- A BPMN attribute the save leaves out is not reported when it held its schema default.
- Diagram layout is matched by the element it draws, whichever diagram plane holds it. Layout that moves to another
  plane, or whose bounds or waypoints change, is `rewritten`, with one warning per element. Everything else diagrams
  hold is compared like the rest of the document: labels, label styles, diagram names and foreign content. An element
  drawn more than once has each drawing matched to one on the same plane first. The ids of diagram interchange elements
  do not matter; the ids of foreign content inside them do.
- A lost XML comment is `dropped`.

A document that draws nothing (no shape or edge in any diagram) is loaded with a generated layout for its first
process: `withGeneratedLayout(xml)` in the shared ClientLib. That adds a `rewritten` warning, and one more for each
other process, which stays in the document but is not drawn. Warnings are still computed against the original
document.

### `save()`

Returns `{ xml }`, the current document. It always returns the XML, whatever the load warnings said. The host decides
whether to send it (see [Saving after a warning](#saving-after-a-warning)). `save` marks the document clean.

### `select(elementId | null)`

Selects the element, or clears the selection for `null`. It returns `false` when no element has that id.

### `getBinding(elementId)` and `setBinding(elementId, binding | null)`

`getBinding` reads the element's `elsa:activityBinding`. It returns `{ activityType, inputs: [{ name, value }] }`,
or `null` when the element has no binding. Each `value` is the input's JSON text, exactly as the document stores it.

`setBinding` writes the binding, or removes it for `null`, and returns `{ applied: true }` or
`{ applied: false, reason }`. When the binding is applied:

- the edit is one undo step;
- every other part of the element is kept, including foreign extension elements and attributes, in their place;
- `change` is raised.

`reason` is one of:

| Reason | When |
| --- | --- |
| `unknown-element` | No element has that id. |
| `not-bindable` | The element cannot carry a binding (see below). |
| `read-only` | The designer was created with `readOnly`. |

`setBinding` throws a `TypeError` for a binding Elsa's import would refuse: one without an activity type, or one that
names an input twice.

An element can carry a binding when Elsa's import would bind it: any task (`bpmn:task` and the task kinds derived
from it), except a `sendTask` or `receiveTask` that names a message (`messageRef`). Elsa runs those as message
publishing and waiting. Gateways, events, subprocesses and call activities cannot carry one.

The binding format is defined by `BpmnActivityBindingFormat` in `Elsa.Bpmn.Interchange`. Its namespace is
`https://elsaworkflows.io/schemas/bpmn/v1`.

### `undo()` and `redo()`

`undo` reverts the last edit and `redo` re-applies the last edit `undo` reverted. Each raises `change`. They return
`false` when there is nothing to undo or redo, and always in a designer created with `readOnly`. The `change` event's
`canUndo` and `canRedo` say whether they would do anything, so a host can enable its undo and redo controls.

### `applyOverlays(statsById | null)` and `clearOverlays()`

`applyOverlays` takes `BpmnElementStats` keyed by element id and replaces the whole overlay. Entries missing from
the new map are removed, and `null` or an empty map clears the overlay, as `clearOverlays` does. Both return
`{ applied, unknownElementIds }`, which are empty after a clear.

### `applyMarkers(markers | null)` and `clearMarkers()` (optional)

Problem markers show what is wrong with an element, such as a reason Elsa's import would refuse the document. A designer
implements both methods or neither. A host checks that `applyMarkers` exists before it calls either. Without them,
the host shows problems some other way, such as in a list.

`applyMarkers` takes a list of `{ elementId, severity, message }` and replaces every marker. `null` or an empty list
clears them, as `clearMarkers` does.

- **Severity.** `severity` is `error` or `warning`. A designer shows a severity it does not know as `error`.
- **Several markers on one element.** The element shows the most severe one and every message.
- **Sequence flows.** A sequence flow can be marked like any other element.
- **Result.** Both methods return `{ applied, unknownElementIds }`, which are empty after a clear.
  - `applied` lists each marked element once, in the order the list first names it.
  - `unknownElementIds` lists the same way each id the designer does not draw, such as a process's own id.
- **Lifetime.**
  - Markers stay until the next `applyMarkers`, `clearMarkers` or `load`.
  - An edit that removes an element removes its markers too.
  - Markers and overlays are independent: neither pair of methods changes what the other shows.
  - Markers work in a designer created with `readOnly` as well.
- **Errors.** `applyMarkers` throws a `TypeError` for a marker whose `elementId`, `severity` or `message` is not a
  string.

### `on(event, listener)`

Subscribes and returns the function that unsubscribes. The events are:

| Event | Payload |
| --- | --- |
| `change` | `{ elementIds, isDirty, canUndo, canRedo }` |
| `selection` | `{ elementIds, primary: { id, type, name? } \| null }` |

Neither event fires while `load` runs. The host learns about a new document from `load`'s result.

### `dispose()`

Releases the designer, its DOM and every listener. It is idempotent.

## Saving after a warning

Before the first save after a `load` that reported any `dropped` warning, the host shows those warnings and saves only
after the user acknowledges them. `rewritten` warnings need no acknowledgement. A new `load` starts over. The designer
takes no part in this: the host owns it (`BpmnSaveGate` in `Elsa.Studio.Bpmn`).

## Hosting a designer in the Blazor Studio

A designer module also ships a registration method, such as `AddBpmnJsDesigner()` in `Elsa.Studio.Bpmn.BpmnJs`, that
enables it with a `BpmnDesignerDescriptor`: its id and the URLs of its module and stylesheet, relative to the page's base
URL. `BpmnDesignerResolver` in `Elsa.Studio.Bpmn` picks the designer a host uses:

| Designers enabled | Result |
| --- | --- |
| None | The default a Studio feature registered with `AddDefaultBpmnDesigner`, if any; otherwise none, and the caller keeps the BPMN view it has. The workflows module (`AddWorkflowsModule`) registers bpmn-js as that default, so a host that enables no designer gets bpmn-js. `BpmnDesignerOptions.DesignerId`, when set, must name the default. |
| One | That one. `BpmnDesignerOptions.DesignerId`, when set, must name it. |
| Several | The one `BpmnDesignerOptions.DesignerId` names. Leaving it unset is an error. |

The default is never counted as enabled: a host that enables a designer, even a different one, uses its enabled
designers and never has to choose between them and the default. A choice that names no enabled designer is an error, and so are two enabled designers that declare the same id.
Enabling the same designer twice enables it once. Errors fail the host at startup, through an `IStartupTask`, rather
than when a BPMN workflow is first opened.

`BpmnCanvasHost` draws with the chosen designer. On its first render it imports `bpmn-canvas-host.js`, which imports the
designer's module on demand and links its stylesheet once. The shim refuses a module whose `designerId` differs from the
descriptor's or whose `contractVersion` is not `1`; the component then shows why. Its methods wrap the designer's and
wait until the designer exists, and it raises `Changed` and `SelectionChanged` for the designer's events. It does not
save on its own; the code around it loads, saves and applies `BpmnSaveGate`. `studio/test/browser/BpmnDesigner/host.spec.ts`
runs the shim against the designer under test.

### In the workflow editor

When the resolver picks a designer and the React Flow canvas is not in effect, the workflow editor edits a
BPMN-imported workflow's whole document in it. Without one, the workflow keeps the read-only canvas and the
"Performed by" panel.

- The workflow definition list offers "Create BPMN workflow". It asks for a name, creates a draft holding a minimal
  process with `POST bpmn/definitions`, and opens it in the editor. Without a designer it is not offered, since nothing
  could draw the new process.
- The document is read with `GET bpmn/definitions/{id}/xml` and saved with `PUT bpmn/definitions/{id}/xml`, carrying
  the `ETag` of the revision the edit was made against. A refused save is reported and never retried. The editor never
  uses the document `GET`/`PUT` pair or the ordinary workflow save for the graph.
- Before the first save of a document whose load returned `dropped` warnings, the editor lists them and saves only if
  the user accepts losing that content. After a save, it names any foreign content the server reports as dropped.
- A save keeps the canvas, its undo history and any edit made while the save ran. The canvas loads the document again
  only when it is read again: on Reload, or when an import replaces it.
- An ordinary save of the workflow's other properties can move the `ETag` without changing the document. The editor
  then reads the document and adopts the new `ETag` if the document is the one the designer was given. A different
  document replaces it when the designer has nothing unsaved; otherwise the next save is refused as a conflict.
- Diagram changes are saved with Save only; auto-save covers the workflow's other properties, as it does for the
  "Performed by" panel. Publishing is refused while the diagram has unsaved changes.
- The panel shows the selected element and, for a task, its "Performed by" section: the activity picker and that
  activity's input editors. It reads the binding with `getBinding` and writes every pick and input edit with
  `setBinding`, so each is one undoable edit saved with the diagram. It reads the binding again after every `change`,
  and keeps the draft being edited when the binding read back is the one it wrote. A refusal is shown, and the binding
  the document kept is read back in place of the refused draft. While the document is read again, and until the designer
  has loaded what the read returned, the editors are hidden and `setBinding` is not called, since loading the document
  discards the designer's edits.
- The viewport tools (Zoom to fit, Center) do nothing on this canvas; this contract does not have them.

### In the viewers

When the resolver picks a designer and the React Flow canvas is not in effect, the definition and instance viewers show
a BPMN-imported workflow's source document in it, created with `readOnly`. Without one, they keep the read-only canvas.

- The designer shows the whole document, subprocess bodies included, so the viewers never open a nested scope for it.
  An instance's state covers the activities of every scope the document shows, and selecting a nested activity from
  the journal selects its element without opening its scope.
- Instance state reaches it through `applyOverlays`, keyed by element id. Each bound activity's state lands on the element
  its `elsa:activityBinding` names; gateways, events and sequence flows bring their own. When an element has both, its
  own counts win, and a fault or block on either shows, as on the read-only canvas. State that arrives while `load`
  runs is applied once it returns.
- A selected element is reported as the activity bound to it, an element bound to none as the scope that holds it, and
  no selection as the root. Selecting an activity from the journal selects its element, and the `selection` event that
  raises is not reported back. An activity selected while `load` runs is selected once it returns.
- A document the designer cannot import shows the error `load` returned, and is loaded again the next time the viewer
  is asked to show it. So is a document whose `load` call threw.
- Zoom to fit and Center do nothing on this canvas, because the contract has no viewport calls.

## Versioning

A change that an existing designer or host could not handle is a new contract version. Examples are removing or
renaming a member, changing a payload's shape, or adding a required option. Adding an optional member is not. A host
treats a warning kind it does not know as `dropped`, so adding a warning kind is not a new version either. Every
designer passes the conformance suite for the version it declares: `studio/test/browser/BpmnDesigner`.
