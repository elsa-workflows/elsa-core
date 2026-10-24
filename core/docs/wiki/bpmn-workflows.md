# BPMN Workflows

Elsa supports running BPMN 2.0 processes as first-class workflow activities. Two modules provide this capability:

| Module | Package | Role |
| --- | --- | --- |
| [Elsa.Bpmn](../../src/modules/Elsa.Bpmn) | `Elsa.Bpmn` | BPMN process execution: `BpmnProcess` activity, work ledger, scope signals, scope host. |
| [Elsa.Bpmn.Interchange](../../src/modules/Elsa.Bpmn.Interchange) | `Elsa.Bpmn.Interchange` | XML import, work binder, and the `elsa:` vendor extension format. |

## Enabling BPMN

```csharp
// Execution support only (use when you build BpmnProcess in code).
elsa.UseBpmn();

// Execution + XML interchange (use when importing .bpmn files).
elsa.UseBpmnInterchange();
```

`BpmnInterchangeFeature` depends on `BpmnFeature`; calling `UseBpmnInterchange()` pulls in both.

### Trying it

The sample host `core/src/apps/Elsa.Server.Web` has BPMN interchange enabled (`.UseBpmnInterchange()` in its `Program.cs`), so it can be used to try the analyze endpoint against a real server. If you have not already trusted the local ASP.NET Core development certificate, run `dotnet dev-certs https --trust` once, otherwise the `curl` calls below will fail TLS verification. Sign in with one of the development users (e.g. `admin`/`password`, see `appsettings.Development.json`) to obtain a bearer token, then call the endpoint from the repository root:

```bash
TOKEN=$(curl -s -X POST https://localhost:5001/elsa/api/identity/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"password"}' | jq -r .accessToken)

curl -s -X POST https://localhost:5001/elsa/api/bpmn/analyze \
  -H "Authorization: Bearer $TOKEN" \
  -F "file=@core/test/integration/Elsa.Bpmn.Interchange.IntegrationTests/Assets/camunda-order-process.bpmn"
```

This returns a JSON body of process ids, element counts, and findings (e.g. informational notes about unbound service tasks or retained vendor extensions).

It also lists in `unboundElementIds` the tasks an import would accept without an activity to perform them (see [Tasks without a binding](#tasks-without-a-binding)); publishing is refused until they are bound.

It also runs the checks an import makes before it saves, and reports in `refusal` why an import of the document would be refused: `{ code, message, elementIds }`, with the same code and message an import responds with, and `null` when the document would import. The checks are capabilities, conditions, bindings and whether the process can run. A refusal of an element's binding, a condition or a capability names the elements it is about; a process that cannot run names none yet. An import stops at its first refusal, so analyze reports that one. A document that declares more than one process is checked only when the `processId` query parameter names the process an import would bind.

## BpmnProcess Activity

`BpmnProcess` is a `Container` that wraps one BPMN scope. It drives the `Bpmn.Semantics` interpreter and applies the continuations the interpreter returns to its own `ActivityExecutionContext`. Key properties:

- **`Process`** — the `BpmnProcessDefinition` from the `Bpmn.Interchange` reader.
- **`WorkBindings`** — a `Dictionary<string, string>` mapping each BPMN binding ref to an Elsa activity id. The scope host uses this map to look up child activity execution contexts when the interpreter signals work completion, faulting, or escalation.
- **`Activities`** — the Elsa activities bound to this scope (one per `BpmnWorkBinding`).
- **`IsRootScope`** — marks the outermost scope as the workflow's entry point, the only scope whose start events can register workflow triggers (see *Start events and workflow triggers* below). `BpmnWorkBinder.Bind` and an import set it on the scope they return; every nested scope the binder produces leaves it `false`.

`BpmnProcess` completes with the interpreter's outcome name — `BpmnInterpreter.DoneOutcomeName` ("Done") normally, or `BpmnInterpreter.CancelledOutcomeName` ("Cancelled") when a cancel end event cancelled a transaction. It does **not** complete with `Outcomes.Default`. Both outcomes are declared flow ports (`[FlowNode(BpmnInterpreter.DoneOutcomeName, BpmnInterpreter.CancelledOutcomeName)]`), so connections from it target one of them explicitly.

### Start events and workflow triggers

A root scope registers one workflow trigger per start event that declares how the process is started:

- A message or signal start registers an `EventStimulus` on the resolved message or signal name, so an ordinary `PublishEvent` with that name starts the workflow.
- A recurring timer start (`<timeCycle>`) registers through `Elsa.Scheduling`'s own `Timer` or `Cron` trigger.

A process whose start events are all plain (none) start events, which covers most Camunda models, registers no trigger at all. It is started directly, through the workflow execution API, and publishes like any other workflow without a trigger. `BpmnProcess` declares this to the trigger indexer through `TriggerIndexingContext.RegistersNoTriggers`, so the indexer does not store the `null`-payload placeholder it keeps for other triggers that return no payloads.

When one of these triggers starts the workflow, the process begins at the start event that registered it, not at its plain start events. The runtime reports which stored trigger fired (`WorkflowExecutionContext.GetTriggerStimulusHash()`, see [Workflow Runtime](workflow-runtime.md#triggers-and-bookmarks)), and the scope begins at the start event whose registered stimulus has that hash, so two message starts on different names, or two timer starts on different intervals, each begin their own path. Message, signal and cron starts are matched by hashing the stimulus the start event registers; an interval timer's stored trigger carries the instant it was indexed, which is part of its hash, so its interval is read back from the stored trigger instead. Start events that register the same stimulus are one trigger, and the process begins at the first of them.

A run that carries no trigger hash, because it was started directly with the process as its trigger activity or was dispatched before the runtime recorded the hash, falls back to the kind of trigger: an event stimulus (`PublishEvent` or `IEventPublisher`) selects the message and signal starts, and a scheduled start selects the timer starts. When start events of that kind register different stimuli, that fallback cannot tell them apart and the process faults with `elsa.bpmn.start.ambiguous-trigger` instead of guessing. A process run directly, through the workflow execution API, still begins at its plain start events.

A start event that declares a message, signal or timer the scope cannot register, such as a timer whose interval is not a positive ISO-8601 duration, is logged and skipped. The scope's other start events still register. If that leaves the scope with nothing to register, publication fails with `Trigger should have a payload`, so the process is not published as though the start had never been declared. Nested scopes (subprocess and event-subprocess bodies, or a scope nested through a `Flowchart`) never register triggers of their own.

Test coverage: `test/integration/Elsa.Bpmn.IntegrationTests/Scenarios/Triggers/BpmnProcessTriggerTests.cs`, `test/integration/Elsa.Bpmn.IntegrationTests/Scenarios/Triggers/BpmnTriggeredStartTests.cs` and `test/integration/Elsa.Bpmn.Interchange.IntegrationTests/Scenarios/Publishing/BpmnStartTriggerPublishTests.cs`.

## Work Ledger

The `BpmnWorkLedger` lives in `ActivityExecutionContext.Properties` of the scope's own context. It is the scope's record of work it has started but not yet finished — a handle-to-context map keyed by scope-local handles.

Rules that matter for contributors:

- Work that **completes or faults** must be removed from the ledger *before* the scope makes its callback, or a rearmed non-interrupting listener can key onto the same `(binding ref, iteration id)` slot and a later teardown hits the finished work.
- Work that **signals** (e.g. escalation from a still-running scope) must remain in the ledger, because removing it causes the interpreter to believe the work has already gone.
- Each `(binding ref, iteration id)` pair must be unique within one scope. Multi-instance bodies share a binding ref and are distinguished by their iteration id.

The ledger is serialized to JSON as part of the scope's `ActivityExecutionContext.Properties` when the workflow suspends, so it survives persistence and resume.

## Work Binding Model

`BpmnWorkBinder` in `Elsa.Bpmn.Interchange` translates a `BpmnProcessDefinition` (from the `Bpmn.Interchange` reader) into a `BpmnProcess`. The seven binding kinds:

| Binding kind | BPMN element | Elsa activity |
| --- | --- | --- |
| `TimerWait` | Timer event | `Delay` (ISO-8601 duration) |
| `MessageWait` | Message catch event | `Event` |
| `SignalWait` | Signal catch event | `Event` |
| `MessagePublish` | Message throw event | `PublishEvent` |
| `CallProcess` | Call activity | `DispatchWorkflow` |
| `NestedProcess` | Embedded subprocess | Recursively bound `BpmnProcess` |
| `UnboundTask` | Task, user, service, script, manual or business rule task; a send or receive task without a resolvable message | Author-declared via `elsa:activityBinding` (see below); otherwise the type's default, or a placeholder (see [Tasks without a binding](#tasks-without-a-binding)) |

BPMN describes *what* a task is for, not *how* to perform it. An unbound task gets its implementation from an `elsa:activityBinding` vendor extension inside the BPMN element's `<extensionElements>`. A send or receive task whose `messageRef` resolves to a named message is not unbound: it binds to `MessagePublish` or `MessageWait` like a message throw or catch event. `elsa:activityBinding` is accepted only on an unbound task; the binder refuses one on any other element, such as a gateway, an event, or a send or receive task bound to its message, because nothing would ever run it.

### BPMN errors

A unit of work that fails raises a BPMN error, and `Bpmn.Semantics` routes it to the error catcher that declares its
code, else to a catch-all (an error boundary event or error event subprocess that declares no code), else the scope
faults with that code. Elsa names the error a failed unit of work raised:

- a `FaultException`, which Elsa's `Fault` activity throws, raises an error coded with its `Code`;
- an embedded subprocess that ended because nothing in it caught an error, including one its own error end event
  threw, raises that error's code in the enclosing scope, so a coded error boundary on the subprocess catches it;
- any other exception raises an error with no code, which only a catch-all catches.

An error end event's code is the `errorCode` of the `<error>` its `errorRef` names, else that error's id; an error end
event without an `errorRef` throws an error with no code. A called workflow's fault reaches its call activity without a
code (see below).
[ADR "Use an Elsa fault's code as its BPMN error code"](../../../docs/adr/2026-10-22-elsa-fault-codes-are-bpmn-error-codes.md)
records this rule.

### Call activity failure

A waiting call activity takes its normal outgoing flow only when the called workflow finishes successfully. When the
called workflow faults, the scope host reports the call activity's work as completed with
`BpmnInterpreter.CallActivityFaultedOutcomeName`, and with `BpmnInterpreter.CallActivityCancelledOutcomeName` when its
final sub-status is `Cancelled`. When the called workflow never starts because its activation strategy denies the new
instance (the dispatch returns `CannotStart`), it reports `BpmnInterpreter.CallActivityDispatchFailedOutcomeName`. The
interpreter routes that failure the way BPMN routes an error with no code:

1. a catch-all error boundary event (one that declares no error code) attached to the call activity fires and its path
   is taken;
2. otherwise the scope's catch-all error event subprocess starts;
3. otherwise the process faults with `bpmn.call-activity.faulted` (or `bpmn.call-activity.cancelled`, or
   `bpmn.call-activity.dispatch-failed`).

Since `Bpmn.Semantics` 0.4.0 an error catcher that declares a code catches only an error with that code, and none of
these failures carries one, so a coded catcher does not catch them.

Only call activities do this. `DispatchWorkflow` itself completes the same way whatever its child's sub-status, so a
`Flowchart` that waits for a child still continues on `Done` after the child faults, and after its activation is denied,
with `CannotStart` in its result. The scope host reads how the child ended, which `DispatchWorkflow` records when it
resumes, instead.

A child cancelled through the cancellation API resumes its waiting parent as well, and reaches this path with
`BpmnInterpreter.CallActivityCancelledOutcomeName`.

Test coverage: `test/integration/Elsa.Bpmn.Interchange.IntegrationTests/Scenarios/Runtime/BpmnCallActivityTests.cs`.

## The `elsa:` Vendor Extension

Namespace URI: `https://elsaworkflows.io/schemas/bpmn/v1`, conventional prefix `elsa`.

```xml
<bpmn:serviceTask id="notify">
  <bpmn:extensionElements>
    <elsa:activityBinding activityType="Elsa.WriteLine">
      <elsa:input name="text">{"typeName":"String","expression":{"type":"JavaScript","value":"getMessage()"}}</elsa:input>
    </elsa:activityBinding>
  </bpmn:extensionElements>
</bpmn:serviceTask>
```

- `activityType` — the Elsa activity type name as the activity registry keys it (`IActivity.Type`, not a CLR name).
- `<elsa:input name="…">` — one element per configured input. The element text is the input value serialized by Elsa's own activity serializer: an `Input<T>`-typed property carries the `{"typeName":…,"expression":…}` wrapper; a plain `[Input]`-attributed property carries the value's own JSON shape (e.g. an array for `Switch.Cases`).
- A duplicate input name is refused. An input name the activity type does not declare is refused. An unregistered `activityType` is refused.

An exported `.bpmn` is self-contained: all binding configuration, including input expressions, travels verbatim in the document. Handle exported files with the same care as the workflow definitions they represent.

**The names in this format are a compatibility surface.** Changing `NamespaceUri`, `BindingElementName`, `ActivityTypeAttributeName`, `InputElementName`, or `InputNameAttributeName` breaks every previously exported `.bpmn` file. Studio and any other tooling that reads or writes this extension must agree on these constants.

### Tasks without a binding

A task that declares no `elsa:activityBinding` is not refused, so a process can be drawn first and implemented later:

- A `userTask` binds to the User Tasks module's `UserTask` activity (`Elsa.UserTask`), titled with the element's name or, without one, its id, when that module is enabled.
- Any other unbound task, and a `userTask` when the User Tasks module is not enabled, binds to a hidden `BpmnUnboundTask` placeholder. The placeholder faults if it runs.

Import and both `PUT` endpoints accept the document and list the placeholder tasks in `analysis.unboundElementIds`; `bpmn/analyze` lists them in `unboundElementIds`. Publishing is refused while any placeholder remains, with a validation error naming the element. Binding a task means declaring its `elsa:activityBinding`.

A default binding is not written into the stored document: exporting the process gives back the task as it was imported, and importing it again applies the default again.

## REST Endpoints

`Elsa.Bpmn.Interchange` registers eight routes, all under `bpmn/`:

| Method & route | Permission | What it does |
| --- | --- | --- |
| `POST bpmn/analyze` | `read:workflow-definitions` | Uploads a single `.bpmn` file (multipart) and returns the Info/Degraded/Dropped findings a read would produce, and the `refusal` an import would raise (optional `processId` query parameter), without persisting anything. |
| `POST bpmn/import` | `write:workflow-definitions` | Uploads a single `.bpmn` file and persists it as a new or updated workflow definition (as a draft; it is not published). Optional form fields: `DefinitionId` (update an existing definition instead of creating one), `Name`, `ProcessId` (required when the document declares more than one process). |
| `POST bpmn/definitions` | `write:workflow-definitions` | Creates a BPMN workflow from scratch: a new draft workflow definition named after the JSON body's required `name`, holding a minimal process — a plain start event connected to an end event, with diagram layout — that a modeler can open and save through the XML `PUT`. Returns `201 Created` with the `Import` response shape, a `Location` header naming the definition's XML, and the `ETag` the first XML `PUT` needs. A missing or blank `name` is `400 Bad Request`. |
| `GET bpmn/definitions/{definitionId}/export` | `read:workflow-definitions` | Writes the workflow definition's BPMN source back out as `.bpmn` XML. Optional `VersionOptions` query parameter (`Latest`, `Published`, or a specific version), defaulting to `Latest`. |
| `GET bpmn/definitions/{definitionId}/document` | `read:workflow-definitions` | Reads the workflow definition's stored BPMN source with the `Bpmn.Model`/`Bpmn.Interchange` reader and returns the whole `bpmnDefinitions` document as the library's own JSON (payload format `1.0.0`), rather than as `.bpmn` XML. That document declares every subprocess but not what is inside one — see *Nested scopes* below. Same refusals as `Export` when the definition was never imported from BPMN or its stored source is stale. Carries an `ETag` response header for the returned revision — see below. |
| `PUT bpmn/definitions/{definitionId}/document` | `write:workflow-definitions` | Accepts a `bpmnDefinitions` JSON document — the shape `GET` on the same route returns — writes it back out as `.bpmn` XML, with every subprocess it still declares keeping the body stored for it (see *Nested scopes* below), and runs it through the same path `Import` runs: analyze, capability check, bind, persist as a new draft, refresh the stored source. Only the activity graph and the `Bpmn:*` custom properties change; the definition's name, description, variables, inputs, outputs, outcomes, options, tool version and any other custom property are carried forward unchanged, unlike `POST bpmn/import`, which stays a whole-definition import (see below). Never edits a published version in place, exactly like `Import`. Returns the same `Id`/`DefinitionId`/`Version`/`Analysis` shape `Import` returns, plus the new `ETag`. Requires an `If-Match` request header — see below. |
| `GET bpmn/definitions/{definitionId}/xml` | `read:workflow-definitions` | Returns the latest version's stored BPMN source as `.bpmn` XML, exactly as stored — unlike `Export`, which writes it out again through the library's writer and can read any version. This is what the BPMN modeler loads. Same refusals as `Export` when the definition was never imported from BPMN or its stored source is stale. Carries the same `ETag` as the document `GET` — see below. |
| `PUT bpmn/definitions/{definitionId}/xml` | `write:workflow-definitions` | Accepts a complete `.bpmn` document as the request body and saves it as the definition's BPMN source: the same import path, refusals, `If-Match` rules and metadata preservation as the document `PUT`, but every subprocess body comes from the posted XML, so nested scopes can be edited — see *The XML endpoints* below. Returns the document `PUT`'s `Id`/`DefinitionId`/`Version`/`Analysis` shape plus `DroppedForeignContent`, and the new `ETag`. |

Both `Analyze` and `Import` require exactly one uploaded file; zero or more than one returns `400 Bad Request`.

### Optimistic concurrency on the document and XML endpoints

`GET` and `PUT` on `bpmn/definitions/{definitionId}/document` and `bpmn/definitions/{definitionId}/xml` exchange the same strong `ETag`, so a client that reads the
document, and someone else writes the definition before it writes its own edit back, cannot silently overwrite that
intervening write. The `ETag` is a SHA-256 hash of what the definition stores: its BPMN document (the `Bpmn:SourceXml`
custom property), its activity graph, its version and its id. Any write that changes the stored document or the
graph therefore invalidates it — another document or XML `PUT`, a `POST bpmn/import` with the same `DefinitionId`, a save of
the draft from the workflow designer — including when an unpublished draft is saved in place under the same version,
which is the common case. The value is opaque and must be sent back verbatim.

Because it is derived from content, identical stored content has an identical `ETag`: a `PUT` that writes back
exactly what is stored returns the same `ETag` `GET` did. The first document `PUT` after a `.bpmn` upload replaces the
uploaded bytes with the writer's own rendering of the same document, so the stored document, and with it the `ETag`,
changes once even when nothing was edited; an XML `PUT` stores the posted text as is. A save that changes only the definition's other properties — its name,
description or variables, say — leaves the document and the graph untouched and does not change the `ETag`; a
document `PUT` leaves those properties as that save left them, since it edits the document, not the rest of the
definition — see the next section. `POST bpmn/import` with the same `DefinitionId` is different: it is a
whole-definition import and resets them from the document, same as it always has.

`PUT` requires an `If-Match` request header carrying the `ETag` a prior `GET` (or `PUT`) returned:

- **Missing, or the wildcard `*`** — `428 Precondition Required`. Neither says which revision the caller is
  replacing (`*` matches whatever is stored), so the endpoint refuses rather than overwrite blindly, before doing any
  import work or persisting anything.
- **Anything other than exactly the definition's current `ETag`** — `412 Precondition Failed`. The comparison is
  exact: a weak (`W/`) tag or a list of tags never matches. Checked once on arrival so a stale client is refused
  before import work runs, and again in the same compare-and-swap that reads the definition's non-BPMN metadata and
  saves — a write that lands in that window is also `412`, not a silent overwrite. The definition was written since
  the caller last read it; `GET` the document again and reapply the edit.
- **Exactly the current `ETag`** — the request proceeds exactly as before, and the response carries the `ETag` of
  the draft as this `PUT` stored it. The draft is prepared first and `WorkflowDefinitionDraftSaving` is
  dispatched before persist so a rejecting handler fails the request before anything is written; handlers
  may mutate that draft, and the compare-and-swap writes that same instance. If name, description or the
  ETag-covered graph moved in that window the swap is `412` rather than silently overwriting the other
  write. A lost race is still `412`. The swap is `IWorkflowDefinitionStore.TryUpdateLatestAsync`, which every
  definition store in this repository implements: in-memory (one lock), EF Core and Dapper (one database
  transaction; Dapper's is serializable) and MongoDB (one session transaction with snapshot read concern). MongoDB
  transactions need a deployment that supports them, such as a replica set: against a standalone server the store
  throws `NotSupportedException` without writing anything, so the `PUT` fails instead of saving an unguarded
  edit. A third-party definition store must implement the method with the same guarantee for this
  endpoint to be concurrency-safe on it.

### The XML endpoints

The `xml` GET/PUT pair is how the BPMN modeler edits a process (ADR *Keep BPMN XML as the source of truth*): it
loads the stored document, edits it, and saves the whole document back. The XML is the source of truth; the Elsa
activity graph is derived from it on every save.

- **The body is the document.** `PUT` reads the request body as BPMN 2.0 XML, whatever its media type. An empty
  body, XML that is not well-formed and XML whose root is not a BPMN `<definitions>` element are `400 Bad Request`.
  Everything else is refused exactly as `Import` and the document `PUT` refuse it.
- **The body is UTF-8.** Send it as UTF-8, or in the Unicode encoding a byte-order mark names. A `charset` other than
  UTF-8 is `415 Unsupported Media Type`, and bytes that are not valid UTF-8 are `400 Bad Request`, rather than text
  saved with its characters silently replaced.
- **Nested scopes are edited, not restored.** Unlike the document `PUT`, nothing is taken from the stored source:
  the posted XML carries every subprocess body, so an element inside one can be changed (#8076).
- **Which process is bound.** A document that declares exactly one process binds it, whatever its id now is, so the
  modeler may rename it. A document that declares more than one binds the process the definition was last bound to
  (`Bpmn:SourceProcessId`), and is `400 Bad Request` when it no longer declares that process.
- **Dropped foreign content.** The response's `DroppedForeignContent` lists the foreign content the stored document
  had that the posted one does not, each as `{ elementId, kind, namespace, name }`: `kind` is `element` or
  `attribute`, and `elementId` is the nearest element id that carried it. Foreign content is anything in a namespace
  other than BPMN's own (model, DI, DC, DD), `xsi`, `xml`, Elsa's `elsa:` binding namespace and the library's vendor
  namespace. A renamed prefix, reordered attributes, changed whitespace or text rewritten as CDATA is not a loss;
  changed content is, and content that moves to another element counts as dropped from the first. Renaming the only
  process does not move its content, since the save binds it whatever its id. The save succeeds either way — the
  modeler asks the user to acknowledge such a loss before it saves, and this list tells a client that did not.

### The document endpoints and the JSON payload format

The `document` GET/PUT pair exists for Studio (W21, part of #7909): Studio holds a BPMN process as the library's own
JSON payload, not as `.bpmn` XML, and editing it — binding a task (W11), moving a shape (W14) — has to write that
JSON back through the same path `Import` uses, or the stored BPMN source drifts out of sync with the definition (see
`BpmnInterchangeDocumentService.SourceVersionCustomPropertyKey` and `SourceGraphHashCustomPropertyKey`) and `Export`
starts refusing with `422`.

The request and response bodies on both routes are the `bpmnDefinitions` document exactly as `Bpmn.Model` serializes
it: property names as `Bpmn.Model`'s own `[JsonPropertyName]` attributes declare them, and any enum as its underlying
integer — **not** Elsa's own API-wide JSON conventions (which add a string-enum converter these bodies must not go
through). A client reading or writing this JSON should use a plain `System.Text.Json` serializer with default
options, not whatever conventions the rest of the Elsa API uses.

**Nested scopes.** `bpmnDefinitions` lists only top-level processes. The body of an embedded subprocess, a
transaction or an event subprocess — its elements, flows and `elsa:` bindings, and any subprocess nested further in —
is not part of that document: the library carries it as the subprocess element's work binding instead, so `GET`
returns each subprocess element with nothing inside it. `PUT` does not take the body from the posted document; it
restores it from the definition's stored BPMN source:

- Every subprocess element the posted document still declares, matched by element id, is written back with the body
  stored for it, exactly as stored — including anything the stored source carries only in a work binding, such as a
  call activity's `vw:waitForCompletion="false"`.
- A subprocess element the posted document no longer declares, or has turned into another kind of element, takes its
  stored body with it; nothing of that body is written back, not even under a new element that reuses its
  `bindingRef`. A subprocess element with no stored body — one this edit adds — is written as posted, empty.
- The subprocess element itself is the posted one: its name, flags, boundary events and multi-instance marker come
  from the document, and a changed or removed marker is written as posted. (`Bpmn.Interchange` before 0.3.1 also kept a
  copy of an interpreted marker inside the body it read, and `Export` wrote it alongside the element's own marker.
  0.3.1 no longer keeps that copy; `PUT` still drops one if a stored body carries it, so it cannot override the
  posted marker or pile up on every write.)
- A subprocess element that has a stored body but no `bindingRef` is refused with `400 Bad Request` before anything
  is persisted: the writer attaches a body through the element's `bindingRef`, so writing it would silently empty
  the subprocess. Send each element back with the `bindingRef` `GET` returned.

Nothing inside a nested scope can therefore be edited through the document endpoints, only its subprocess element
and its layout: the document's BPMN DI carries the shapes of nested elements too, and `PUT` writes them as posted. A
definition without stored BPMN source has no bodies to restore; the `If-Match` precondition only matches a stored state
a successful `GET` or `PUT` described, and both need the source.

**A top-level call activity's call options survive an unrelated `PUT`.** A call activity's options — currently just
`vw:waitForCompletion="false"`, the fire-and-forget flag — live only on its `CallProcess` work binding, the same way a
nested scope's contents do; `bpmnDefinitions` cannot carry them either. `PUT` reuses the stored options for a
top-level call activity element the posted document still declares under the same element id and the same
`calledElement`, so editing an unrelated task's binding does not silently turn a fire-and-forget call into a waiting
one on the next save. A call activity whose `calledElement` has changed does not inherit the old options: it is a call
to a different process, so it binds fresh (waiting, BPMN's default) instead. A call activity nested inside a kept
subprocess already keeps its options as part of that subprocess's whole stored body, described above.

A document that declares more than one `<process>` is re-imported against the same `processId` it was originally
imported with — recorded on the workflow definition the first time it is imported, whether from `Import` or from a
`document` `PUT`, so an edit to a multi-process document does not have to name the process again on every save.

**The document `PUT` edits the BPMN document, not the whole definition.** Only the activity graph the newly bound
document produces and the `Bpmn:*` custom properties (`SourceXml`, `SourceVersion`, `SourceProcessId`,
`SourceGraphHash`) change; the
definition's name, description, variables, inputs, outputs, outcomes, options, tool version and every other custom
property are carried forward exactly as they stood before the `PUT`. This is what lets Studio's binding UX (elsa-
studio#1001) save a binding change through this endpoint without silently resetting metadata the author set some
other way — a renamed definition, variables added on the draft, a description. `POST bpmn/import` with a
`DefinitionId` is unaffected by this: it remains a whole-definition import, building the definition from the
uploaded document alone and replacing all of the above, exactly as it always has.

### Capability refusal at import

A BPMN document can declare behaviour (e.g. certain multi-instance or event-subprocess shapes) that needs a host
capability this deployment's runtime does not implement. `Import` checks this — for the whole document, including
nested processes — before persisting anything, and refuses with `422 Unprocessable Entity` naming the missing
capabilities and the offending element ids, rather than persisting a definition that only fails the first time it
runs. `Analyze` never performs this check, since it does not persist; a document that `Analyze` reports cleanly can
still be refused by `Import` on capability grounds.

### Conditions

Elsa evaluates a sequence flow's standard BPMN `<conditionExpression>` with its own expression engine, against the
process's data, the same way it evaluates any other expression in a workflow (ADR "Evaluate BPMN conditions with Elsa
expressions", `docs/adr/2026-10-20-elsa-evaluates-bpmn-conditions.md`). `Bpmn.Semantics` decides which conditions are
asked, in what order, and what the answers mean for routing; Elsa only answers each one.

- **Language.** The expression's own `language` attribute, else the document's `definitions/@expressionLanguage`,
  selects the Elsa expression type: `javascript`, `csharp` or `liquid`, case-insensitively, written as that name or as
  a URI whose last path segment is that name. JavaScript is used when neither names a language. Any other language,
  such as XPath, JUEL or FEEL, is refused at import (see below); it is never mapped onto one of these. A language whose
  Elsa module the host has not enabled fails when the condition is evaluated.
- **Data.** The expression runs in the BPMN scope's expression context, so it sees the workflow's input and the
  variables of the process and of every enclosing scope, for example `getInput('amount') > 1000` in JavaScript.
  Camunda's `${...}` syntax is JUEL, not JavaScript, so such an expression fails unless it is rewritten.
- **Routing.** An exclusive gateway takes the first flow, in declaration order, whose condition holds; a flow with no
  condition that is not the default counts as true. An inclusive gateway and an activity take every flow whose
  condition holds, plus every flow with no condition. The default flow is taken only when nothing else is. Without one, the scope faults: a gateway with
  `bpmn.gateway.no-flow-selected`, any other element with `bpmn.flow.none-taken`.
- **Result.** A condition must evaluate to a boolean; nothing is converted, so `1` or `'yes'` is not true. Liquid
  renders text, so a Liquid condition answers with the text `true` or `false`.
- **Failure.** A condition that throws or does not evaluate to a boolean faults its BPMN scope (the process or the
  subprocess) with `bpmn.condition.failed`, naming the flow and the expression's error or result, which Elsa records
  as an incident like any other fault.

`Import` and the document `PUT` refuse two kinds of condition, with `422 Unprocessable Entity`, before persisting
anything. The check covers the whole document, including the bodies of nested subprocesses, `data.elementIds` lists
the offending sequence flow ids, sorted, and no opt-in overrides either refusal:

- `bpmn.import.condition-language-unsupported`: an expression in a language Elsa does not evaluate.
- `bpmn.import.condition-conflict`: a flow that carries both a `vw:conditionOutcome` and an author-written
  `conditionExpression`, because either precedence rule would route some documents differently from what their
  author meant. The readable `outcome == '...'` expression the library's writer emits beside an outcome is the outcome
  restated, so it does not count.

A flow conditional on an outcome name through the library's own `vw:conditionOutcome` attribute keeps working as
before. `Analyze` does not refuse.

### Process build refusal

Each subprocess body runs as its own scope, with its own graph. `Import` and the document `PUT` build the graph of the
process and of every subprocess body it can reach, at any depth, through `Elsa.Bpmn.Hosting.BpmnProcessGraphs`, and
refuse a document `Bpmn.Semantics` refuses to build, with `422 Unprocessable Entity` and the code
`bpmn.import.process-invalid`, before persisting anything. Examples are a cycle on which nothing can wait, a default
flow that does not leave its element, and an event subprocess body without exactly one start event. The message is the
library's own and names the elements. A run does the same when it starts: a root BPMN scope builds every body it can
reach before anything runs, so a definition stored before this check faults at its start rather than when a token
first reaches the broken subprocess. At run time this also covers a BPMN process composed into other work, such as a
`Flowchart` or an `If` branch: the root finds it in the workflow graph, through whatever ports its wrapper has.

### Duplicate element id refusal

BPMN requires every element id to be unique within a document. `Import` and the document and XML `PUT` refuse, with
`422 Unprocessable Entity`, a document that repeats one — most often a subprocess nested inside another subprocess
that reuses its parent's id. This is not just an ordinary validation rule: reading or writing such a document walks
into a nested process by matching the repeated id back out of a flat binding list, in three different places (this
service's own capability walk, the work binder, and the interchange library's own writer), and each of those walks
would otherwise recurse without ever terminating and crash the process outright — .NET cannot catch a
`StackOverflowException`. These endpoints check this before any of that recursion runs. `Analyze` does not refuse
such a repeat, since the plain read it does never walks a nested process this way to begin with. It does refuse, with
the same code, two flow nodes that share an id inside one process or subprocess body: the library's reader cannot
read such a document at all.

### Export's limitation

`Export` does not reconstruct a `.bpmn` document from the Elsa activity graph a definition runs — that would discard
everything the reader retained on import (foreign extension elements, foreign attributes, unrecognized children, BPMN
DI layout). Instead, it returns exactly the document the most recent successful import stored. This has a real
consequence for one kind of edit: **an edit made through Elsa's own designer, without going back through BPMN, is not
reflected in what `Export` returns** — the graph moves on, but the stored source still describes the document as it
stood before that edit. Rather than silently returning that pre-edit document, `Export` and the document `GET` refuse
it as stale — see below.

An edit made through the `document` `PUT` endpoint above is different: it re-imports, so the stored source and the
graph move together, and **`Export` reflects the edit immediately afterward.**

`Export` also refuses outright, with `422 Unprocessable Entity`, rather than silently returning a stale or wrong
document, in three situations:

- The definition does not currently carry BPMN source — either it was never imported from BPMN, or a later save
  replaced its custom properties wholesale (BPMN source travels on the same `CustomProperties` dictionary a workflow
  edit can overwrite).
- The definition's activity graph has changed since the source was recorded. `Import` records a SHA-256 hash of the
  graph (`BpmnInterchangeDocumentService.SourceGraphHashCustomPropertyKey`, `Bpmn:SourceGraphHash`) at the moment it
  stores the source, and once a definition carries that marker, it alone decides staleness: `Export`/the document
  `GET` refuse exactly when the current graph's hash no longer matches it, regardless of whether the definition's
  version has also changed. That cuts both ways. An unpublished draft is saved in place (same row, same version), so
  a designer save that edits a bound activity's inputs — as Studio's binding UX (elsa-studio#1001) does — moves the
  graph without moving the version, and is refused as stale even though the version alone would have missed it. The
  other way round, publishing a definition and then making a metadata-only save — a rename, a variable change — bumps
  it to a new draft version without touching the graph, and is *not* refused: the version moved, but the stored
  source still describes the graph exactly. One practical consequence: a document `GET` performed after a designer
  save that edits the graph returns `422` — Studio has to re-import (or PUT a fresh document) rather than edit a
  document that no longer describes the current graph — but a `GET` after a rename or other metadata-only save keeps
  returning `200`.
- The definition has changed — by version — since the source was recorded, and it carries no graph-hash marker to
  decide staleness by instead. This is the whole test for a definition imported before that marker existed; once one
  exists, it takes over from the version check entirely, as above.

A missing `definitionId` returns `404 Not Found`.

### Error codes on refusals

**Every code below is a compatibility surface.** A client (Studio's own BPMN designer among them) matches on the
`code`, and on the `data` fields a code documents, rather than on the message — the message may be reworded without
notice.

`bpmn/import`, `bpmn/definitions/{id}/export`, and the document and XML `GET`/`PUT` endpoints report most of their
BPMN-specific refusals as an additive envelope alongside the usual FastEndpoints error body shape (`statusCode`,
`message`, `errors`):

```json
{
  "statusCode": 422,
  "message": "One or more errors occurred!",
  "errors": { "generalErrors": ["<the human-readable message>"] },
  "code": "bpmn.import.capability-unsupported",
  "data": { "capabilities": ["ScopeSignalling"], "elementIds": ["Task_1"] }
}
```

`statusCode`, `message` and `errors` are exactly what FastEndpoints' own `ErrorResponse` would have sent for
`AddError("<the human-readable message>")` — a client that only reads those three keys today (e.g. Studio's
`ValidationApiExceptionExtensions.GetValidationErrorsFromContent`) keeps working unchanged. `code` and `data` are
additive. `data` is omitted when a code carries none. This deployment does not use FastEndpoints'
`ProblemDetails` response, and this envelope is written by the endpoint itself rather than by replacing
FastEndpoints' process-wide `Config.ErrOpts.ResponseBuilder`, which would have reshaped every endpoint's error
response, not just these — see `Elsa.Bpmn.Interchange.Endpoints.Bpmn.BpmnErrorResponse`'s remarks.

Not every 4xx these endpoints send carries a code: a plain `404 Not Found` for a `definitionId` that does not exist,
a `400 Bad Request` from malformed JSON, an XML `PUT` body that is not BPMN 2.0 XML or an unparseable `VersionOptions`, a `400 Bad Request` for a document
that names more than one process without saying which, and a document `PUT`'s `400 Bad Request` for a subprocess
element that has a stored body but no `bindingRef`, are uncoded.

| Code (`Elsa.Bpmn.Interchange.BpmnErrorCodes`) | Sent by | Status | `data` |
| --- | --- | --- | --- |
| `bpmn.import.capability-unsupported` | `POST bpmn/import`, document and XML `PUT` | 422 | `capabilities: string[]` (missing capability names), `elementIds: string[]` (offending element ids, combined across every missing capability) |
| `bpmn.import.binding-invalid` | `POST bpmn/import`, document and XML `PUT` | 422 | `elementIds: string[]` (the element whose binding is refused), when the refusal is about one element. A task with no binding at all is not refused; see [Tasks without a binding](#tasks-without-a-binding) |
| `bpmn.import.duplicate-element-id` | `POST bpmn/import`, document and XML `PUT`; `POST bpmn/analyze` for two flow nodes sharing an id in one body | 422 | `elementIds: string[]` (the duplicated ids) |
| `bpmn.import.process-invalid` | `POST bpmn/import`, document and XML `PUT` | 422 | — |
| `bpmn.import.condition-language-unsupported` | `POST bpmn/import`, document and XML `PUT` | 422 | `elementIds: string[]` (the sequence flows whose expression is in a language Elsa does not evaluate, across the whole document including nested subprocesses, sorted and distinct) |
| `bpmn.import.condition-conflict` | `POST bpmn/import`, document and XML `PUT` | 422 | `elementIds: string[]` (the sequence flows that carry both an outcome and an author-written expression, across the whole document including nested subprocesses, sorted and distinct) |
| `bpmn.export.not-imported` | `GET .../export`, document and XML `GET` | 422 | — |
| `bpmn.export.source-stale` | `GET .../export`, document and XML `GET` | 422 | — |
| `bpmn.export.source-version-unknown` | `GET .../export`, document and XML `GET` | 422 | — |
| `bpmn.document.not-found` | document and XML `PUT` | 404 | — |
| `bpmn.document.precondition-required` | document and XML `PUT` | 428 | — |
| `bpmn.document.precondition-failed` | document and XML `PUT` | 412 | — |

See each constant's XML doc in `Elsa.Bpmn.Interchange.BpmnErrorCodes` for exactly which situation it names.
`bpmn.export.source-version-unknown` is not reachable through `Import` or the document and XML `PUT` themselves — all three
always record a source version alongside the source text — only through custom properties edited or migrated some
other way; it is kept, and coded, as a defence against that combination arising.

## Execution State Persistence

The BPMN interpreter's execution state (`BpmnExecutionState`) and the scope's `BpmnWorkLedger` are both serialized as JSON strings in `ActivityExecutionContext.Properties` when the workflow suspends. The state is pruned before each persist: consumed tokens are removed so the serialized size stays bounded regardless of how many evaluations a long-running scope has processed.

Test coverage: `test/integration/Elsa.Bpmn.IntegrationTests/Scenarios/HostPort/BpmnPersistenceTests.cs` proves that state size stays flat across a multi-iteration loop.

## Diagnostics Projection

Under Option A, only bound work (a task, a nested scope) gets its own Elsa activity id. A gateway, an intermediate event or a sequence flow is a decision the interpreter made internally, and the interpreter records every one of them in `BpmnExecutionState.Diagnostics`. `BpmnScopeHost` projects each new diagnostic onto the scope's own execution log — as an `AddExecutionLogEntry` call on the scope's own `ActivityExecutionContext`, never a child's — before the state is pruned, since pruning caps `Diagnostics` at 200 entries and a diagnostic that falls off the cap can never be projected from persisted state afterward. A resumed scope does not re-project a diagnostic a previous evaluation already turned into a journal entry: the last diagnostic id projected is tracked as a high-water mark in the scope's own memory, next to its execution state and work ledger.

- **Event name** — the diagnostic kind's own enum member name (e.g. `TokenEmitted`, `Joined`, `Faulted`). `Elsa.Bpmn.Hosting.BpmnDiagnosticEventNames` documents every one of them as a public constant, so Studio has one place to mirror instead of depending on the library's integer enum values.
- **Source** — always `"BPMN"` (`BpmnDiagnosticEventNames.Source`).
- **Payload** — `Elsa.Bpmn.Hosting.BpmnDiagnosticLogPayload`, serialized camelCase like every other execution log payload: `diagnosticId`, `elementId`, `flowId`, `tokenId`, `kind` (the enum member name, again as a string) and `details`, carried verbatim from the diagnostic. Studio keys its overlay on `elementId` (and, for a decision about a flow, `flowId`); neither is folded into the message.
- **Not projected** — only a diagnostic that names neither an element nor a flow, which is the scope's own terminal `Completed` diagnostic; it is already journaled as the activity's own lifecycle. Everything else is projected, including a start event's own token emission (keyed on the start element, which Studio's overlay lights up) and an error or cancel boundary's token emission when it fires without an inbound flow (keyed on the boundary element).
- **Volume** — measured with the 12-iteration sequential multi-instance loop, each iteration after the first adds two projected entries (`Consumed`, `Scheduled`), 106 diagnostics in total, so no per-kind filter is applied beyond the exclusion above; journal growth is proportional to the process's work, like any activity's journal.

Test coverage: `test/integration/Elsa.Bpmn.IntegrationTests/Scenarios/HostPort/BpmnDiagnosticsProjectionTests.cs`.

## Composing BPMN Into an Elsa Workflow

A `BpmnProcess` is a `Container` and can be nested inside any Elsa composite activity (e.g. a `Flowchart`). The workflow that hosts it is responsible for marking the outermost scope as the entry point (`IsRootScope = true`). Nested BPMN scopes — embedded subprocesses, event subprocesses — are themselves `BpmnProcess` instances bound as child work by the binder and need no special treatment from the containing workflow.

To find code fast, from the repository root:

```bash
rg "class BpmnProcess" core/src/modules
rg "class BpmnWorkLedger" core/src/modules
rg "elsa:activityBinding" core/test/
```
