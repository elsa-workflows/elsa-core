# BPMN view model fixtures

Each fixture is a **pair**:

| File | What it is |
| --- | --- |
| `<name>.bpmn` | the source BPMN 2.0 document, verbatim |
| `<name>.activity.json` | the root `Elsa.BpmnProcess` activity JSON elsa-core produces when it imports that document |

The pair is the view model's two inputs (see `../README.md`): the JSON is the typed source of truth
for structure and bindings, the `.bpmn` is read for BPMN DI geometry and pool names only.

`camunda-order-process.process.json` is the older, single-property fixture the generated-types test
(`../__tests__/process-payload.test-d.ts`) asserts against. It is the `"process"` property of
`camunda-order-process.activity.json` and is kept because that test's narrative is about the
`process` payload specifically.

## How the `.activity.json` files were produced

Not by hand. Each one is the exact `WorkflowDefinition.StringData` that
`Bpmn.Interchange.BpmnInterchangeDocumentService.ImportAsync` produces for the paired `.bpmn`,
serialized by `Elsa.Workflows.Serialization.Serializers.JsonActivitySerializer` — the same serializer
a GET of a workflow definition goes through. Reproduce it with a throwaway console app that
references elsa-core's `Elsa.Bpmn.Interchange`, `Elsa` and `Elsa.Testing.Shared.Integration`
projects:

1. build an `IServiceCollection` with `.AddElsa(elsa => elsa.UseWorkflowManagement().UseBpmnInterchange())`;
2. `await provider.PopulateRegistriesAsync()`;
3. resolve `BpmnInterchangeDocumentService` and call
   `ImportAsync(xml, definitionId: null, name: null, processId: null, cancellationToken)`;
4. write `result.ImportResult.WorkflowDefinition.StringData`, pretty-printed, to `<name>.activity.json`.

Regenerate all of them the same way after a `Bpmn.Model` / `Bpmn.Interchange` version bump: the
payload is the library's format, not this repository's, so a fixture that no longer matches what the
library emits is the thing the fixture tests exist to catch.

## Where each document came from, and what it covers

`camunda-order-process`, `non-interrupting-error-event-subprocess` and `publish-gate-process` are
copies of elsa-core's `test/integration/Elsa.Bpmn.Interchange.IntegrationTests/Assets/*.bpmn`. The
rest were written here to cover element kinds those miss.

| Fixture | Covers |
| --- | --- |
| `camunda-order-process` | A Camunda-authored document: `camunda:*` extension elements and foreign attributes, `bpmn:documentation`, an `elsa:activityBinding`, and a full BPMNDI section. |
| `publish-gate-process` | A recurring timer start event. **No BPMNDI section** → fallback layout. |
| `non-interrupting-error-event-subprocess` | A document the reader partly refuses (see *Dropped constructs*). **No BPMNDI section** → fallback layout. |
| `gateway-routing` | All four gateways, a conditional flow (`vw:conditionOutcome`) and a default flow, an event-based gateway racing a timer and a message catch event, edge and shape labels, `isMarkerVisible`. |
| `task-kinds-and-lanes` | All eight task kinds, `callActivity`, a collaboration with one pool and two lanes, DI on the plane of the *collaboration* rather than of the process. |
| `subprocess-boundary-events` | An embedded subprocess with a collection multi-instance marker, an event subprocess (`triggeredByEvent`, with its `listenerBindingRef`), three boundary events on one host — interrupting timer, non-interrupting message, interrupting error — an escalation throw event and a terminate end event. |
| `transaction-compensation` | A transaction subprocess, a cancel end event and a cancel boundary event, a compensation boundary event associated to a `isForCompensation` handler, and DI for an association edge the payload has no id for. |

## Dropped and degraded constructs

elsa-core's reader reports on an Info / Degraded / Dropped ladder, and the fixtures assert against
what the payload **actually contains**, not against what the `.bpmn` says. The differences worth
knowing about:

* **`non-interrupting-error-event-subprocess`**: the `OnError` event subprocess is *Dropped* —
  "error events are always interrupting per BPMN" — along with the bindings in its body. The
  captured payload therefore has three elements (`Start_1`, `Settle`, `End_1`) and two flows, and
  no subprocess at all. That is deliberate on elsa-core's side, and the fixture test asserts the
  drop rather than fighting it.
* **`subprocess-boundary-events`**: a collection multi-instance only reads if its collection is a
  *declared* variable of the enclosing container, so the process declares
  `<vw:variable name="orderLines"/>`; without it the reader degrades the element to "no loop
  characteristics". The reader also tallies `<multiInstanceLoopCharacteristics>` as a dropped child
  element while simultaneously consuming it into `loopCharacteristics` — an artefact of the
  element-count tally, not a real loss.
* **Associations have no id in the payload.** `<bpmn:association>` is recorded as
  `compensationHandlerElementId` on the compensation boundary event, so there is nothing to look its
  BPMNEdge up by. `transaction-compensation.bpmn` carries such an edge, and the view model reports it
  as an `unresolved-di-edge` diagnostic: expected, and the reason `BpmnViewAssociation` carries no
  waypoints.

## `unbound-task-process.bpmn` is deliberately absent

elsa-core's fourth test asset cannot be captured: `BpmnWorkBinder` **refuses** to bind a task with no
`elsa:activityBinding`, so `ImportAsync` throws

> BPMN element 'Unbound_1' of process 'unbound-process' is a 'serviceTask': the document says what it
> is for, not how to perform it, and nothing binds it to an Elsa activity.

and no payload exists to capture. An unbound task therefore never reaches Studio through import; it
arises only from editing the document in Studio (W11/W14) before binding the new element, which is
what W8 refuses to publish. `../__tests__/view-model.test.ts` covers that state by removing a
`workBindings` entry, and a `bindingRef`, from a captured payload — the two shapes such an edit
produces — rather than by shipping a `.bpmn` no test can load.
