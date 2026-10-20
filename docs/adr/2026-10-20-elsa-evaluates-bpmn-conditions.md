# Evaluate BPMN conditions with Elsa expressions

Date: 2026-10-20

## Status

Accepted

## Context

`Bpmn.Semantics` has no expression language by design. A sequence flow can only be conditional on an outcome name, set
with the library's own `conditionOutcome` attribute, and is taken when the work before it completes with that outcome.
A standard `<conditionExpression>` is read as Degraded and turned into an unconditional flow. Import accepts the
document anyway; only Analyze reports it. A typical model imported from Camunda Modeler or bpmn.io therefore routes
differently from its diagram, without an error.

Multi-instance `completionCondition`, `standardLoopCharacteristics` and conditional events need the same thing: a way
to evaluate an expression against the process's data at the moment the interpreter needs a decision. The library
already calls back into the host for one thing, reading variables through `IBpmnVariableReader`.

Elsa already has an expression engine with JavaScript, C# and Liquid, evaluated against workflow variables and inputs.

## Decision

Conditions are evaluated by the host, with Elsa's expression engine.

- `Bpmn.Semantics` gains a host callback for condition evaluation, alongside `IBpmnVariableReader`. It receives the
  expression text and language and returns true or false. This change is requested upstream in valence-works/bpmn.
- `Elsa.Bpmn` implements the callback with `IExpressionEvaluator`. The language follows BPMN's precedence: a
  `conditionExpression`'s own `language` attribute overrides the document's `definitions/@expressionLanguage`. Either
  value selects an Elsa expression type, `javascript`, `csharp` or `liquid`, written as that name, case-insensitively,
  or as a URI whose last path segment is that name. JavaScript is used when neither value is present. A language Elsa
  does not support is refused at import with a coded error, never mapped to another language.
- A condition that fails to evaluate faults the BPMN scope it belongs to (the process or subprocess) with the
  expression's error, which Elsa records as an incident like any other expression failure. An exclusive or inclusive
  gateway whose conditional flows all evaluate false and that has no default flow faults the same way, as the BPMN
  specification requires. An activity in the same situation also faults. The specification lets the token end there,
  but Elsa treats it as an error so that a process never stops silently. On an exclusive gateway, an outgoing flow with
  no condition that is not the default counts as true, as it does in Camunda.
- `conditionOutcome` keeps working as it does today. A sequence flow that carries both a `conditionOutcome` and an
  author-written `conditionExpression` is refused at import with a coded error, because either precedence rule would
  route some documents differently from what their author meant. The `outcome == '...'` expression that the library
  writes for an outcome flow is the outcome restated, not a second condition, so it does not count.
- The same callback serves `completionCondition`, standard loops and conditional events once the library supports them.

Until the callback ships, import refuses a document whose flows carry `conditionExpression`, with a coded error,
instead of running it with the conditions removed. This refusal is an exception to the `acceptDegraded` opt-in in
[Accept unbound tasks in BPMN drafts and refuse silent degradation](2026-10-20-bpmn-import-policy.md): the opt-in does
not override it, because a process with its conditions removed routes differently from its diagram on every run, not
only in an edge case an author could reasonably accept.

## Consequences

- Imported models route as drawn.
- BPMN conditions are written in the same expression languages as the rest of an Elsa workflow, and are portable to
  other engines only when the expression itself is.
- Expressions run inside the workflow host with the same trust as any other Elsa expression.
- The program depends on an upstream library release for this capability.

## Alternatives considered

- **FEEL or JUEL.** Matches Camunda, but adds a second expression language to Elsa with its own runtime and editor.
  Out of scope for v2.
- **Bind each gateway to an Elsa decision activity.** Works today without the library, but every gateway would need an
  extra binding the model does not show.
