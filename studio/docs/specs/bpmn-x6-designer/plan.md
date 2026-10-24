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
3. **Basic editing:** moving and resizing with their layout, with labels and waypoints following; deleting, with what
   depends on the deleted element; inline name editing; keyboard undo and redo.
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
