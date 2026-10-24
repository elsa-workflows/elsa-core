# X6 BPMN designer: plan

The steps that build the designer [spec.md](spec.md) describes, one pull request each. The read-only X6 BPMN view
(`studio/src/modules/Elsa.Studio.Workflows/DiagramDesigners/Bpmn/`) keeps working throughout.

## Steps

1. **Modeling kernel, part 1, and this spec and plan.** In `Elsa.Studio.Bpmn/ClientLib/src/modeling/`:
   - reading and writing a document, with load warnings;
   - the element and plane indexes, and the plane view;
   - the command stack;
   - the `updateProperties` and `setBinding` commands, and the session that applies the contract's rules;
   - Vitest round trips over the contract fixtures and Elsa's BPMN test assets, and the boundary test.

   bpmn-moddle becomes a runtime dependency of the shared ClientLib, pinned exactly to 10.3.1, the version the bpmn-js
   designer resolves.
2. **The `Elsa.Studio.Bpmn.X6` module.** It implements the whole contract on the kernel and draws the document, with no
   editing tools yet. CI runs the shared conformance suite (`contract`, `host`, `preservation`) and the import
   equivalence check against it, as against bpmn-js. It draws content a collapsed subprocess hides (`hiddenBy`) on a
   plane of its own, as bpmn-js does.
3. **Basic editing**, in four pull requests:
   - 3a. moving shapes, with their labels, contents, boundary events and edges following, and keyboard undo and redo;
   - 3b. resizing;
   - 3c. deleting, with what depends on the deleted element;
   - 3d. inline name editing.
4. **The palette, creating elements, and replacing an element with another type.**
5. **Connections:** sequence-flow rules, associations, edge editing, and attaching and detaching boundary events.
6. **Pools, lanes and message flows.** Includes a check that a document the X6 designer saves opens in bpmn-js with no
   warnings.
7. **Subprocesses at every depth:** expanded subprocesses as containers, collapsed subprocesses on their own plane,
   toggling between them, event subprocesses and transactions.
8. **The properties panel:** conditions, timers, message references, `cancelActivity` and the other BPMN properties
   beyond the binding, in the contract's `propertiesContainer` (spec.md, decision 2).
9. **The designer's limits document, packaging, and the host opt-in documentation.** Completes "Done when".
10. **Retiring the read-only view** (spec.md, decision 1).

## Risk

The conformance suite tests the contract's API, not editing gestures, so it passes from step 2. Editing quality rests on
the X6 designer's own Playwright tests, the kernel tests, and the cross-designer check in step 6.

## Step 1 verification

- The shared ClientLib's `npm test` (typecheck and Vitest) passes, and the bpmn-js designer's `npm run build`
  (typecheck, including the shared sources, and bundle budget) passes with its bundle unchanged.
- Every contract fixture and Elsa test asset reads, and its save reads again with no warnings and saves unchanged.
- The kernel's load warnings for each of them equal the bpmn-js designer's, as `preservation.spec.ts` records them with
  `BPMN_DESIGNER_OUTPUT` set. The semantic part of each save is byte-identical to the bpmn-js designer's; only diagram
  elements differ in order and placement (spec.md, "Reading").

## Step 2 verification

- The shared conformance suite passes against the X6 designer's build: `contract`, `host` and `preservation`, with the
  optional problem markers. CI runs it beside the bpmn-js run, and runs the Elsa import equivalence check on the X6
  designer's round trips.
- `x6.spec.ts` checks what the designer draws: one cell per drawn element named by its id, external labels, the plane a
  collapsed subprocess opens (from `select`, its button and the breadcrumb), selection by click, instance-state badges,
  problem markers on shapes and edges, and that nothing can be moved.
- The bundle stays within its budget, and `Elsa.Studio.Bpmn.Tests` covers `AddX6BpmnDesigner`.

## Step 3a verification

- Kernel tests (`modeling-move.test.ts`) cover:
  - a moved shape, and its straight and bent edges staying orthogonal;
  - edge labels following the edge's midpoint;
  - an edge between two moved shapes;
  - an expanded subprocess's content and boundary events;
  - a participant's lanes and content, with lane membership unchanged;
  - refusals;
  - two moves in one compound;
  - undo back to the original XML, and redo.
- `x6.spec.ts` covers:
  - a drag that moves a shape in the document, with its label following during the drag;
  - the change event with the edges it rerouted;
  - selection of the dragged shape;
  - keyboard undo and redo;
  - labels and edges that cannot be dragged;
  - a read-only designer where nothing moves and the keyboard does nothing.
- The shared conformance suite still passes against the X6 designer.

## Step 3b verification

- Kernel tests (`modeling-resize.test.ts`) cover:
  - a resized expanded subprocess, with its boundary event and edge ends following the sides they are nearer;
  - edge ends on a side that moves;
  - a text annotation and its association;
  - two resizes in one compound;
  - refusals: tasks, events, boundary events, collapsed subprocesses, sizes under the minimum, bounds that leave out
    the content;
  - undo back to the original XML.
- `x6.spec.ts` covers:
  - handles shown only on a resizable selected shape;
  - a corner drag that resizes in the document, with the boundary event following;
  - the resize stopping at the content;
  - undo;
  - no handles when read-only.

## Step 3c verification

- Kernel tests (`modeling-delete.test.ts`) cover:
  - a task deleted with its boundary event, flows, data association, lane reference and drawings;
  - a gateway default cleared;
  - a collapsed subprocess deleted with its content and its own plane's diagram;
  - a data object deleted with its last reference;
  - associations deleted with their task;
  - several elements in one undo step;
  - an element renamed earlier in the same compound still deleted;
  - refusals;
  - undo back to the original XML, and redo.
- `x6.spec.ts` covers:
  - Delete removing the selected element and its flows, and clearing the selection;
  - undo bringing them back;
  - Delete with no selection, and when read-only, doing nothing.
