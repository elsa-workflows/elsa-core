# `src/designer/bpmn` — the X6 BPMN adapter

Draws the canvas-neutral BPMN view model (`../../bpmn`) with `@antv/x6`. X6 is what users get unless
they opt into React Flow (`DesignerOptions.UseReactFlow`, default false), so BPMN ships on it first.

**Read-only.** No dragging, no resizing, no connecting, no adding. That is W14. Selection, double
click, zoom to fit, centre and the instance-state overlay are all that is here.

## What belongs here, and what does not

The review test from the design (elsa-workflows/elsa-core#7909 §4, D2): **if it would be identical
in a React Flow adapter, it belongs in `../../bpmn`, not here.** Element kinds, geometry, waypoints,
bindings, boundary attachment, lane membership, diagnostics and the fallback layout all arrive
already resolved on the view model, and nothing in this folder re-derives any of them.

What *is* here is canvas-specific by that same test: SVG path data for the BPMN markers, X6 shape
registrations, painting order, and the X6 attribute shapes they are expressed in. A React Flow
adapter would render an event definition as a React component, not as a `d` string.

## The split, and why

`buildBpmnX6Cells(viewModel)` is a **pure function** returning plain `Node.Metadata` and
`Edge.Metadata` — no `Graph`, no DOM, `@antv/x6` imported for types only. That is what lets
`__tests__/cells.test.ts` run the whole mapping against every W9a fixture and assert the
correspondence a screenshot cannot: one node per element, one edge per flow, every boundary event
still naming its host, every default flow still carrying its marker, every cell id unique.
`mount.ts` is the remaining sliver that genuinely needs a live graph.

`graph-options.ts` is separate for the same reason: read-only is the property the whole adapter
rests on, and as a value it can be asserted rather than reviewed.

## Files

| File | What it does |
| --- | --- |
| `cells.ts` | `buildBpmnX6Cells`. The mapping. Pure. |
| `shapes.ts` | `registerBpmnShapes()`: the X6 node/edge shapes and the `bpmnWaypoint` anchor. |
| `graph-options.ts` | The read-only `Graph` configuration, as a value. |
| `glyphs.ts` | SVG path data for event definitions, task types, gateways and activity markers. |
| `stats.ts` | Which badge one element's instance state resolves to. |
| `palette.ts` | The CSS custom properties the shapes paint with; see `css/designer.bpmn.css`. |
| `constants.ts` | Shape names, cell id prefixes, painting order. No imports, so `cells.ts` stays loadable without X6. |
| `mount.ts` | Creating the graph, loading cells, updating stats, selection. |
| `graph-registry.ts` | Live BPMN graphs by container id. |
| `dotnet-bpmn-designer.ts` | The interop calls back into .NET. |

The exported JS interop surface is `../api/bpmn-designer.ts`; `../designer.ts` re-exports it with
the rest of `../api`.

## Three decisions worth knowing about

**Boundary events are not X6 children of their host.** A boundary event sits astride its host's
border, so embedding it would give X6 a child overlapping its own parent's body and change which of
the two a click on that border resolves to. `parent`/`children` is also X6's *model* hierarchy, not
a drawing hint, and using it would make the canvas assert a containment the document does not have.
The host is recorded in the node's `data` instead. Lanes and pools hold nothing for the same reason,
and are `pointerEvents: 'none'` so they never intercept a click meant for what is drawn over them.

**Edges are pinned to the document's own waypoints.** The `bpmnWaypoint` anchor expresses each end
as an offset from the node's own rectangle, so the line is the author's line to the pixel and still
belongs to the two nodes it joins. X6's built-in `topLeft` anchor cannot do this: it measures the
node's *rendered* bounding box, which for a BPMN node includes the name drawn outside the shape.

**The overlay is replaced, never merged.** `updateBpmnElementStats` sets every element's stats from
the map it is given, so an element the map stops mentioning loses its badge. A fault that outlives
the instance that raised it is the failure that looks most like success.
