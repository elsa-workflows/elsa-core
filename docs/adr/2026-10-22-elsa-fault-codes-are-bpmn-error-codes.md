# Use an Elsa fault's code as its BPMN error code

Date: 2026-10-22

## Status

Accepted

## Context

Since `Bpmn.Semantics` 0.4.0, BPMN error catchers match by code. An error boundary event or error event subprocess
whose `errorRef` names an error code catches only an error with that code. A catch-all, which declares no code,
catches any error, but only when no catcher declares the error's code. A work fault therefore needs a code before a
coded catcher can ever catch it, and the library takes that code from the host on each fault it reports.

Elsa has no BPMN-specific way for work to say which error it raised. It already has a coded fault: `FaultException`
carries a `Code`, a `Category` and a `Type`. Elsa's `Fault` activity throws it with an author-chosen code. A few
modules also throw it with fixed codes for their own failures, for example `HttpFaultCodes.NoHttpContext` and
`AlterationFaultCodes.PlanNotFound`.

## Decision

The BPMN scope host reports a failed unit of work's error code as follows:

- **`FaultException`:** the code is its `Code`. A BPMN author raises a business error by binding a task to Elsa's
  `Fault` activity and giving it the code that the catcher's `<error>` declares.
- **`BpmnScopeFaultException`:** the code is its `ErrorCode`. That exception means an embedded subprocess ended
  because nothing inside it caught an error, including an error its own error end event threw. The code is that
  error's, so a coded boundary on the subprocess catches it in the enclosing scope.
- **Any other exception:** the error has no code, and only a catch-all catches it.

The library matches the code exactly, after trimming surrounding whitespace, and treats a blank code as none. Elsa
does not map, prefix or normalize codes beyond that.

A called workflow's fault reaches its call activity without a code, because Elsa's incidents do not record one. Only
a catch-all on the call activity, or in its scope, catches it. The library can already carry such a code
(`BpmnWorkCompletedRequest.ErrorCode` with the call activity's `Faulted` outcome). Recording it on the called
workflow, so Elsa can pass it on, is a separate change.

## Consequences

- A BPMN model can route Elsa's business faults to specific handlers with standard BPMN error catchers, with no new
  activity or attribute.
- Every `FaultException` code is a BPMN error code, not only codes from the `Fault` activity. A module's fixed codes,
  such as `NoHttpContext`, can be caught by a catcher that declares them. That is useful when it is intended, and an
  accidental match only happens if a model declares exactly that string.
- A plain exception never satisfies a coded catcher, so an unexpected failure goes to a catch-all or faults the scope.
  It is never mistaken for a business error.
- `BpmnScopeFaultException.ErrorCode` is part of the hosting contract. Changing how the code is derived changes which
  catcher runs.

## Alternatives considered

- **A dedicated channel.** For example, a `BpmnErrorCode` on the `Fault` activity, or a `BpmnErrorException`, so that
  only faults deliberately aimed at BPMN carry a code. That rules out accidental matches, but it adds public API and a
  second way to name a fault, and the risk it removes is an exact string collision. Revisit if a collision shows up
  in practice.
- **No codes.** Report every fault without a code. Coded catchers would then never fire for Elsa work, which is what
  0.4.0's matching rule made visible.
