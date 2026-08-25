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

`Elsa.Bpmn.Interchange` registers three routes, all under `bpmn/`:

| Method & route | Permission | What it does |
| --- | --- | --- |
| `POST bpmn/analyze` | `workflows/definitions:view` | Uploads a single `.bpmn` file (multipart) and returns the Info/Degraded/Dropped findings a read would produce, without persisting anything. |
| `POST bpmn/import` | `workflows/definitions:write` | Uploads a single `.bpmn` file and persists it as a new or updated workflow definition (as a draft; it is not published). Optional form fields: `DefinitionId` (update an existing definition instead of creating one), `Name`, `ProcessId` (required when the document declares more than one process). |
| `GET bpmn/definitions/{definitionId}/export` | `workflows/definitions:view` | Writes the workflow definition's BPMN source back out as `.bpmn` XML. Optional `VersionOptions` query parameter (`Latest`, `Published`, or a specific version), defaulting to `Latest`. |

Both `Analyze` and `Import` require exactly one uploaded file; zero or more than one returns `400 Bad Request`.

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
DI layout). Instead, it returns exactly the document `Import` stored at import time. This has a real consequence
until BPMN-aware editing exists in Studio: **edits made through Elsa's own designer, after import, are not reflected
in what `Export` returns.**

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

## Composing BPMN Into an Elsa Workflow

A `BpmnProcess` is a `Container` and can be nested inside any Elsa composite activity (e.g. a `Flowchart`). The workflow that hosts it is responsible for marking the outermost scope as the entry point (`IsRootScope = true`). Nested BPMN scopes — embedded subprocesses, event subprocesses — are themselves `BpmnProcess` instances bound as child work by the binder and need no special treatment from the containing workflow.

To find code fast:

```bash
rg "class BpmnProcess" src/modules
rg "class BpmnWorkLedger" src/modules
rg "elsa:activityBinding" test/
```
