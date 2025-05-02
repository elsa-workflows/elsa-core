# 4. Token-Centric Flowchart Execution Model

Date: 2025-05-01

## Status

Accepted

## Context

Elsa Workflows’ existing flowchart implementation uses execution‐count heuristics to decide when to fire joins. In scenarios with loops or partial forks (e.g. XOR gateways), this approach is brittle:

- Loop‐back edges never emit the “forward” token, so AND-joins stall.
- Counting executions across iterations can cause premature or missed firings.
- Users cannot control join semantics without deep framework changes.

We need a more robust, flexible model that:

1. Guarantees correct behavior for loops, XOR splits, and parallel forks.
2. Lets activity authors or workflow designers opt into different join modes.
3. Remains performant and easy to reason about.

## Decision

Adopt a **token-centric** execution model for the `Flowchart` activity:

1. **Emit tokens**
    - After each activity completes, create an immutable token for each outbound connection:
        - Carries `FromActivityId`, `Outcome`, `ToActivityId`.
        - Includes a `JoinKind` hint (StaticAnd, DynamicAnd, or Or).
        - Loop-back edges are always `Or`.
        - Activities implementing `IJoinHintProvider` (e.g. `FlowSwitch`) supply `DynamicAnd`; all others default to `StaticAnd`.

2. **Store tokens**
    - Persist the token list in `ActivityExecutionContext.Properties["FlowchartTokens"]`.

3. **Join and schedule**
    - Periodically (on each completion) gather all unconsumed tokens by target.
    - For each target activity:
        1. **Explicit override**: if the target implements `IJoinOverride`, honor its `JoinKind`.
        2. **OR-join**: consume & schedule once per `Or` token.
        3. **Dynamic-AND**: wait until *all* `DynamicAnd` tokens for that target have arrived, consume one each, then schedule once.
        4. **Static-AND**: wait until ≥1 `StaticAnd` token exists per *defined* inbound connection, consume them, then schedule once.

4. **Completion**
    - When no tokens remain and no activities are scheduled, the flowchart completes.

### Sequence Diagram

```mermaid
sequenceDiagram
    participant A as Activity A
    participant F as Flowchart
    participant B as Activity B

    A-->>F: Completed(outcome="Done")
    F->>F: Emit Token(A→B,JoinKind.StaticAnd)
    F->>F: Save tokens
    F->>F: Check join for B
    alt Static-AND satisfied
      F-->>B: Schedule B
    else
      Note over F: Wait for more tokens
    end
