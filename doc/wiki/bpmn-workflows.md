# BPMN Workflows

Elsa supports running BPMN 2.0 processes as first-class workflow activities. Two modules provide this capability:

| Module | Package | Role |
| --- | --- | --- |
| [Elsa.Bpmn](../../src/modules/Elsa.Bpmn) | `Elsa.Bpmn` | BPMN process execution: `BpmnProcess` activity, work ledger, scope signals, scope host. |
| [Elsa.Bpmn.Interchange](../../src/modules/Elsa.Bpmn.Interchange) | `Elsa.Bpmn.Interchange` | XML import, work binder, and the `elsa:` vendor extension format. |

## Enabling BPMN

```csharp
// Execution support only (use when you build BpmnProcess in code).
elsa.AddBpmn();

// Execution + XML interchange (use when importing .bpmn files).
elsa.AddBpmnInterchange();
```

`BpmnInterchangeFeature` depends on `BpmnFeature`; calling `AddBpmnInterchange()` pulls in both.

## BpmnProcess Activity

`BpmnProcess` is a `Container` that wraps one BPMN scope. It drives the `Bpmn.Semantics` interpreter and applies the continuations the interpreter returns to its own `ActivityExecutionContext`. Key properties:

- **`Process`** — the `BpmnProcessDefinition` from the `Bpmn.Interchange` reader.
- **`WorkBindings`** — a `Dictionary<string, string>` mapping each BPMN binding ref to an Elsa activity id. The scope host uses this map to look up child activity execution contexts when the interpreter signals work completion, faulting, or escalation.
- **`Activities`** — the Elsa activities bound to this scope (one per `BpmnWorkBinding`).
- **`IsRootScope`** — left `false` on every scope the binder produces; the caller sets it to `true` to mark the outermost scope as a workflow entry point.

`BpmnProcess` completes with the interpreter's outcome name (e.g. `BpmnInterpreter.DoneOutcomeName`). It does **not** complete with `Outcomes.Default`, so connections from it must target explicit outcome ports.

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
| `GET bpmn/definitions/{definitionId}/document` | `read:workflow-definitions` | Reads the workflow definition's stored BPMN source with the `Bpmn.Model`/`Bpmn.Interchange` reader and returns the whole `bpmnDefinitions` document as the library's own JSON (payload format `1.0.0`), rather than as `.bpmn` XML. Same refusals as `Export` when the definition was never imported from BPMN or its stored source is stale. Carries an `ETag` response header for the returned revision — see below. |
| `PUT bpmn/definitions/{definitionId}/document` | `write:workflow-definitions` | Accepts a `bpmnDefinitions` JSON document — the shape `GET` on the same route returns — writes it back out as `.bpmn` XML, and runs it through the same path `Import` runs: analyze, capability check, bind, persist as a new draft, refresh the stored source. Never edits a published version in place, exactly like `Import`. Returns the same `Id`/`DefinitionId`/`Version`/`Analysis` shape `Import` returns, plus the new `ETag`. Requires an `If-Match` request header — see below. |

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
document `PUT`, like `Import`, resets those properties regardless.

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
`BpmnInterchangeDocumentService.SourceVersionCustomPropertyKey`) and `Export` starts refusing with `422`.

The request and response bodies on both routes are the `bpmnDefinitions` document exactly as `Bpmn.Model` serializes
it: property names as `Bpmn.Model`'s own `[JsonPropertyName]` attributes declare them, and any enum as its underlying
integer — **not** Elsa's own API-wide JSON conventions (which add a string-enum converter these bodies must not go
through). A client reading or writing this JSON should use a plain `System.Text.Json` serializer with default
options, not whatever conventions the rest of the Elsa API uses.

A document that declares more than one `<process>` is re-imported against the same `processId` it was originally
imported with — recorded on the workflow definition the first time it is imported, whether from `Import` or from a
`document` `PUT`, so an edit to a multi-process document does not have to name the process again on every save.

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
stood before that edit.

An edit made through the `document` `PUT` endpoint above is different: it re-imports, so the stored source and the
graph move together, and **`Export` reflects the edit immediately afterward.**

`Export` also refuses outright, with `422 Unprocessable Entity`, rather than silently returning a stale or wrong
document, in two situations:

- The definition does not currently carry BPMN source — either it was never imported from BPMN, or a later save
  replaced its custom properties wholesale (BPMN source travels on the same `CustomProperties` dictionary a workflow
  edit can overwrite).
- The definition has changed — by version — since the source was recorded, meaning the stored BPMN text no longer
  corresponds to the current definition.

A missing `definitionId` returns `404 Not Found`.

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
