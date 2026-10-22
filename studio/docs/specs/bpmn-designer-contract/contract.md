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

- `imported` is `false` when the document could not be shown at all; `error` then says why.
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
  plane, or whose bounds or waypoints change, is `rewritten`, with one warning per element. Diagram element ids do not
  matter.
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

## Versioning

A change that an existing designer or host could not handle is a new contract version. Examples are removing or
renaming a member, changing a payload's shape, or adding a required option. Adding an optional member is not. A host
treats a warning kind it does not know as `dropped`, so adding a warning kind is not a new version either. Every
designer passes the conformance suite for the version it declares.
