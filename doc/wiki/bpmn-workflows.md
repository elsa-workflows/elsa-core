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

The sample host `src/apps/Elsa.Server.Web` has BPMN interchange enabled (`.UseBpmnInterchange()` in its `Program.cs`), so it can be used to try the analyze endpoint against a real server. If you have not already trusted the local ASP.NET Core development certificate, run `dotnet dev-certs https --trust` once, otherwise the `curl` calls below will fail TLS verification. Sign in with one of the development users (e.g. `admin`/`password`, see `appsettings.Development.json`) to obtain a bearer token, then call the endpoint:

```bash
TOKEN=$(curl -s -X POST https://localhost:5001/elsa/api/identity/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"password"}' | jq -r .accessToken)

curl -s -X POST https://localhost:5001/elsa/api/bpmn/analyze \
  -H "Authorization: Bearer $TOKEN" \
  -F "file=@test/integration/Elsa.Bpmn.Interchange.IntegrationTests/Assets/camunda-order-process.bpmn"
```

This returns a JSON body of process ids, element counts, and findings (e.g. informational notes about unbound service tasks or retained vendor extensions).

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

A start event that declares a message, signal or timer the scope cannot register, such as a timer whose interval is not a positive ISO-8601 duration, is logged and skipped. The scope's other start events still register. If that leaves the scope with nothing to register, publication fails with `Trigger should have a payload`, so the process is not published as though the start had never been declared. Nested scopes (subprocess and event-subprocess bodies, or a scope nested through a `Flowchart`) never register triggers of their own.

Test coverage: `test/integration/Elsa.Bpmn.IntegrationTests/Scenarios/Triggers/BpmnProcessTriggerTests.cs` and `test/integration/Elsa.Bpmn.Interchange.IntegrationTests/Scenarios/Publishing/BpmnStartTriggerPublishTests.cs`.

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
| `UnboundTask` | Service / send / user / script task | Author-declared via `elsa:activityBinding` (see below) |

BPMN describes *what* a task is for, not *how* to perform it. An unbound task gets its implementation from an `elsa:activityBinding` vendor extension inside the BPMN element's `<extensionElements>`.

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

## REST Endpoints

`Elsa.Bpmn.Interchange` registers five routes, all under `bpmn/`:

| Method & route | Permission | What it does |
| --- | --- | --- |
| `POST bpmn/analyze` | `read:workflow-definitions` | Uploads a single `.bpmn` file (multipart) and returns the Info/Degraded/Dropped findings a read would produce, without persisting anything. |
| `POST bpmn/import` | `write:workflow-definitions` | Uploads a single `.bpmn` file and persists it as a new or updated workflow definition (as a draft; it is not published). Optional form fields: `DefinitionId` (update an existing definition instead of creating one), `Name`, `ProcessId` (required when the document declares more than one process). |
| `GET bpmn/definitions/{definitionId}/export` | `read:workflow-definitions` | Writes the workflow definition's BPMN source back out as `.bpmn` XML. Optional `VersionOptions` query parameter (`Latest`, `Published`, or a specific version), defaulting to `Latest`. |
| `GET bpmn/definitions/{definitionId}/document` | `read:workflow-definitions` | Reads the workflow definition's stored BPMN source with the `Bpmn.Model`/`Bpmn.Interchange` reader and returns the whole `bpmnDefinitions` document as the library's own JSON (payload format `1.0.0`), rather than as `.bpmn` XML. That document declares every subprocess but not what is inside one — see *Nested scopes* below. Same refusals as `Export` when the definition was never imported from BPMN or its stored source is stale. Carries an `ETag` response header for the returned revision — see below. |
| `PUT bpmn/definitions/{definitionId}/document` | `write:workflow-definitions` | Accepts a `bpmnDefinitions` JSON document — the shape `GET` on the same route returns — writes it back out as `.bpmn` XML, with every subprocess it still declares keeping the body stored for it (see *Nested scopes* below), and runs it through the same path `Import` runs: analyze, capability check, bind, persist as a new draft, refresh the stored source. Only the activity graph and the `Bpmn:*` custom properties change; the definition's name, description, variables, inputs, outputs, outcomes, options, tool version and any other custom property are carried forward unchanged, unlike `POST bpmn/import`, which stays a whole-definition import (see below). Never edits a published version in place, exactly like `Import`. Returns the same `Id`/`DefinitionId`/`Version`/`Analysis` shape `Import` returns, plus the new `ETag`. Requires an `If-Match` request header — see below. |

Both `Analyze` and `Import` require exactly one uploaded file; zero or more than one returns `400 Bad Request`.

### Optimistic concurrency on the document endpoints

`GET` and `PUT` on `bpmn/definitions/{definitionId}/document` exchange a strong `ETag`, so a client that reads the
document, and someone else writes the definition before it writes its own edit back, cannot silently overwrite that
intervening write. The `ETag` is a SHA-256 hash of what the definition stores: its BPMN document (the `Bpmn:SourceXml`
custom property), its activity graph, its version and its id. Any write that changes the stored document or the
graph therefore invalidates it — another document `PUT`, a `POST bpmn/import` with the same `DefinitionId`, a save of
the draft from the workflow designer — including when an unpublished draft is saved in place under the same version,
which is the common case. The value is opaque and must be sent back verbatim.

Because it is derived from content, identical stored content has an identical `ETag`: a `PUT` that writes back
exactly what is stored returns the same `ETag` `GET` did. The first `PUT` after a `.bpmn` upload replaces the
uploaded bytes with the writer's own rendering of the same document, so the stored document, and with it the `ETag`,
changes once even when nothing was edited. A save that changes only the definition's other properties — its name,
description or variables, say — leaves the document and the graph untouched and does not change the `ETag`; a
document `PUT` leaves those properties as that save left them, since it edits the document, not the rest of the
definition — see the next section. `POST bpmn/import` with the same `DefinitionId` is different: it is a
whole-definition import and resets them from the document, same as it always has.

`PUT` requires an `If-Match` request header carrying the `ETag` a prior `GET` (or `PUT`) returned:

- **Missing, or the wildcard `*`** — `428 Precondition Required`. Neither says which revision the caller is
  replacing (`*` matches whatever is stored), so the endpoint refuses rather than overwrite blindly, before doing any
  import work or persisting anything.
- **Anything other than exactly the definition's current `ETag`** — `412 Precondition Failed`, checked before any
  import work and before anything is persisted. The comparison is exact: a weak (`W/`) tag or a list of tags never
  matches. The definition was written since the caller last read it; `GET` the document again and reapply the edit.
- **Exactly the current `ETag`** — the request proceeds exactly as before, and the response carries the `ETag` of
  the draft as this `PUT` stored it.

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
  from the document, and a changed or removed marker is written as posted. (`Bpmn.Interchange` 0.2.0 also keeps a
  copy of an interpreted marker inside the body it reads; `PUT` drops that copy so it cannot override the posted
  marker or pile up on every write. `Export` still writes that copy alongside the element's own marker.)
- A subprocess element that has a stored body but no `bindingRef` is refused with `400 Bad Request` before anything
  is persisted: the writer attaches a body through the element's `bindingRef`, so writing it would silently empty
  the subprocess. Send each element back with the `bindingRef` `GET` returned.

Nothing inside a nested scope can therefore be edited through the document endpoints, only its subprocess element
and its layout: the document's BPMN DI carries the shapes of nested elements too, and `PUT` writes them as posted. A
definition without stored BPMN source has no bodies to restore; the `If-Match` precondition only matches a stored state
a successful `GET` or `PUT` described, and both need the source.

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

`bpmn/import`, `bpmn/definitions/{id}/export`, and the document `GET`/`PUT` endpoints report most of their
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
a `400 Bad Request` from malformed JSON or an unparseable `VersionOptions`, a `400 Bad Request` for a document
that names more than one process without saying which, and a document `PUT`'s `400 Bad Request` for a subprocess
element that has a stored body but no `bindingRef`, are uncoded.

| Code (`Elsa.Bpmn.Interchange.BpmnErrorCodes`) | Sent by | Status | `data` |
| --- | --- | --- | --- |
| `bpmn.import.capability-unsupported` | `POST bpmn/import`, document `PUT` | 422 | `capabilities: string[]` (missing capability names), `elementIds: string[]` (offending element ids, combined across every missing capability) |
| `bpmn.import.binding-invalid` | `POST bpmn/import`, document `PUT` | 422 | — |
| `bpmn.export.not-imported` | `GET .../export`, document `GET` | 422 | — |
| `bpmn.export.source-stale` | `GET .../export`, document `GET` | 422 | — |
| `bpmn.export.source-version-unknown` | `GET .../export`, document `GET` | 422 | — |
| `bpmn.document.not-found` | document `PUT` | 404 | — |
| `bpmn.document.precondition-required` | document `PUT` | 428 | — |
| `bpmn.document.precondition-failed` | document `PUT` | 412 | — |

See each constant's XML doc in `Elsa.Bpmn.Interchange.BpmnErrorCodes` for exactly which situation it names.
`bpmn.export.source-version-unknown` is not reachable through `Import` or the document `PUT` themselves — both
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

To find code fast:

```bash
rg "class BpmnProcess" src/modules
rg "class BpmnWorkLedger" src/modules
rg "elsa:activityBinding" test/
```
