# Accept unbound tasks in BPMN drafts and refuse silent degradation

Date: 2026-10-20

## Status

Accepted

## Context

Two import rules work against the goal of designing BPMN in Elsa.

Import refuses a task that carries no `elsa:activityBinding` (`BpmnWorkBinder`, `bpmn.import.binding-invalid`). The
publish check (`ValidateBpmnProcessBindings`) already refuses to publish an unbound task, so the import refusal protects
nothing. It does block the normal way of working: draw the process first, then decide how each task is performed. A
model from an analyst cannot be imported until every task has been given `elsa:` XML by hand.

The reverse problem exists for semantics. The reader degrades or drops elements it does not support (an expression
condition becomes unconditional, an error end becomes a plain end, a signal throw disappears) and import accepts the
result. The process then behaves differently from its diagram without an error.

Two BPMN task types also have obvious Elsa counterparts that today must be bound by hand: `userTask` to the `UserTask`
activity of `Elsa.UserTasks`, and `scriptTask` to its own script.

## Decision

Be lenient about what is unfinished and strict about what would change meaning.

- **Unbound tasks.** Import and save accept tasks without a binding. Each unbound task is bound to a placeholder activity
  that faults if it ever runs. `ValidateBpmnProcessBindings` is extended to refuse that placeholder type, alongside the
  missing activities and `NotFoundActivity` instances it refuses today, so publishing is refused while any placeholder
  remains.
- **Degraded and Dropped findings.** Import and save refuse a document with any Degraded or Dropped finding, with a coded
  error listing the elements. A caller can accept them explicitly with an `acceptDegraded` option. The finding is then
  recorded on the definition and shown in Studio. The one exception is an expression condition on a sequence flow:
  until Elsa evaluates conditions, it is refused even with `acceptDegraded`, as
  [Evaluate BPMN conditions with Elsa expressions](2026-10-20-elsa-evaluates-bpmn-conditions.md) records.
- **Default bindings.** When a task has no binding, its type supplies one:
  - `userTask` binds to `UserTask` when `Elsa.UserTasks` is enabled.
  - `scriptTask` binds to its script, run as an Elsa expression in the language its `scriptFormat` names. The format
    maps to `javascript`, `csharp` or `liquid` by the same rules as condition languages: the bare name or a URI ending
    in it, case-insensitively, with JavaScript when `scriptFormat` is absent. A script task with an unsupported format
    or an empty script is treated as an unbound task: it gets the placeholder and an Info finding naming the element.
    This is not a Degraded finding and needs no `acceptDegraded`, because nothing the document says is changed or
    lost: the script is kept in the stored XML, and the task is unfinished work like any other unbound task.
    Publishing refuses the process until the author binds the task.
  - `sendTask` and `receiveTask` that name a message keep the message publish and message wait bindings the reader
    already gives them.
  - `serviceTask`, `businessRuleTask`, `manualTask`, plain `task`, and a send or receive task without a message have no
    default and stay unbound until the author chooses an activity.

## Consequences

- An analyst's model imports as a draft and can be bound in Studio.
- No process runs differently from its diagram without the author having accepted it.
- `bpmn.import.binding-invalid` is no longer sent for a missing binding; it remains for invalid ones. Clients matching
  on the code for missing bindings must change, so the change is noted in the release notes.
- The placeholder activity is a new, hidden activity type.

## Alternatives considered

- **Keep refusing unbound tasks at import.** Keeps one rule, but leaves import useless for models that are not yet bound.
- **Warn on Degraded findings instead of refusing.** Warnings at import are easy to miss, and the cost of missing one is
  a process that routes wrongly in production.
