# 6. Adoption of Explicit Merge Modes for Flowchart Joins

Date: 2025-09-30

## Status

Accepted

## Context

The Flowchart activity serves as a container for orchestrating workflows through activities connected via directed edges, using a token-based model for control flow. Initially, the execution logic relied on a combination of counter-based and token-based approaches, with implicit handling for merging paths (joins). However, this led to inconsistencies:

- **Premature Scheduling in Forks**: In conditional forks with converges, untaken branches (e.g., false decision outcomes) allowed downstream activities to execute unexpectedly, violating expected blocking behavior.
- **Stalling in Loops**: Strict token checks for all inbounds broke loops by consuming entry tokens and failing to reschedule on backward connections.
- **Inconsistent Merges in Complex Flows**: In workflows with switches and multiple branches (e.g., MatchAny modes), dead paths (untaken defaults) caused hangs under strict rules but proceeded under approximations, leading to conflicting expectations.

The root issue was the lack of explicit, configurable semantics for joins, relying instead on heuristics (e.g., inbound connection count >1). This made behavior opaque and error-prone, especially in unstructured flowcharts. Inspired by BPMN gateway semantics (e.g., AND-join for strict sync, OR-join for partial), we needed a clearer model to balance safety (blocking on required paths) and flexibility (proceeding on dead paths).

## Decision

We refine the Flowchart's token-based execution logic (`OnChildCompletedTokenBasedLogicAsync`) to use an explicit `MergeMode` enum on activities. This eliminates null/default fallbacks, making behaviors self-documenting and configurable via activity properties.

- **MergeMode Enum Definition** (in `MergeMode.cs`):
  ```csharp
  namespace Elsa.Workflows.Activities.Flowchart.Models;

  public enum MergeMode
  {
      /// <summary>
      /// No special merging; use approximation that proceeds after all upstream sources complete, ignoring dead paths.
      /// Suitable for flexible, unstructured merges where optional branches shouldn't block.
      /// </summary>
      None,

      /// <summary>
      /// Strict wait for tokens from all forward inbound connections. Blocks on dead/untaken paths.
      /// Use for required synchronization points.
      /// </summary>
      Converge,

      /// <summary>
      /// Schedule on each arriving token, allowing multiple executions if supported.
      /// </summary>
      Stream,

      /// <summary>
      /// Schedule on the first arriving token, block or cancel others.
      /// </summary>
      Race
  }
  ```

- **Key Changes in Flowchart Execution**:
    - **Token Emission and Consumption**: On activity completion, emit tokens only for active outcomes (matching connections). Consume inbound tokens post-execution.
    - **Scheduling Logic**: For each outbound connection, evaluate the target's `MergeMode` (via `GetMergeModeAsync`). Handle each mode explicitly in a switch statement.
    - **Graph Reliance**: Use `FlowGraph` for forward inbound connections (acyclic); backward connections (e.g., loops) are handled naturally without inflating counts.
    - **Dead Path Handling**: Varies by mode (strict blocking in Converge; approximation in None).
    - **Loop Support**: Converge mode checks inbound count >1 to schedule immediately for sequentials/loops (<=1 forwards).
    - **Cancellation and Purging**: Retained for races and overall cleanup.

- **Implementation Snippet** (from `Flowchart` partial class; full code in PR):
  ```csharp
  switch (mergeMode)
  {
      case MergeMode.Stream:
      case MergeMode.Race:
          // Existing logic: Schedule on arrival, block others for Race.
          // ...
          break;

      case MergeMode.Converge:
          // Strict check: Wait for all forward inbounds if >1; else schedule immediately.
          var inboundConnections = flowGraph.GetForwardInboundConnections(targetActivity);
          if (inboundConnections.Count > 1)
          {
              var hasAllTokens = inboundConnections.All(inbound => /* token check */);
              if (hasAllTokens) await flowContext.ScheduleActivityAsync(...);
          }
          else
          {
              await flowContext.ScheduleActivityAsync(...);
          }
          break;

      case MergeMode.None:
      default:
          // Approximation: Schedule if no unconsumed tokens to inbound sources.
          var inboundConnections = flowGraph.GetForwardInboundConnections(targetActivity);
          var hasUnconsumed = inboundConnections.Any(inbound => /* source token check */);
          if (!hasUnconsumed) await flowContext.ScheduleActivityAsync(...);
          break;
  }
  ```

### Functional Overview
Flowchart execution starts with scheduling the root/start activity. As activities complete:
1. Emit tokens for matching outbound connections.
2. Consume the activity's inbound tokens.
3. For each emitted token's target:
    - Fetch its `MergeMode`.
    - Apply mode-specific logic to decide scheduling.
4. Purge consumed tokens and complete the flowchart if no pending work.

This ensures acyclic forward flow with support for backward loops, using tokens to track control without global state beyond the list.

### Merge Modes Explained
Each mode defines how tokens from multiple inbounds are synchronized:

- **None (Default/Flexible Merge)**:
    - **Behavior**: Schedules if there are no unconsumed tokens *to the sources* of inbounds (i.e., all upstream activities have completed, treating dead paths as "done").
    - **When to Use**: Flexible merges in unstructured flows; optional/exclusive branches (e.g., switch defaults) shouldn't block.
    - **Scenarios**:
        - **Forks with Untaken Paths**: Proceeds after active branches (e.g., in complex switch with dangling default).
        - **Loops**: Schedules on loop-back tokens (backward ignored in forward inbounds).
        - **Dead Paths**: Ignores untaken outcomes; no blocking.
        - **Example**: In a switch with MatchAny, untaken default doesn't hang the merge.

- **Converge (Strict Synchronization)**:
    - **Behavior**: Requires unconsumed, non-blocked tokens from *all* forward inbounds. For <=1 forward, schedules immediately (loop/sequential friendly).
    - **When to Use**: Required "all must happen" joins; block if any branch untaken.
    - **Scenarios**:
        - **Conditional Forks**: Blocks downstream if e.g., decision returns false and subsequent activities are connected to the true branch.
        - **Loops**: Works if forward inbounds <=1; reschedules on backward tokens.
        - **Dead Paths**: Blocks (desired for safety).
        - **Example**: Converge after parallel approvals—only proceed if all complete.

- **Stream (Per-Token Execution)**:
    - **Behavior**: Schedules on each arriving token; may allow multiple concurrent executions of the target.
    - **When to Use**: Streaming merges where each branch triggers independently (e.g., event streams).
    - **Scenarios**:
        - **Forks**: Executes target per branch.
        - **Loops**: Executes per iteration.
        - **Dead Paths**: Ignores; only active tokens trigger.
        - **Example**: Logging each branch outcome separately.

- **Race (First-Wins)**:
    - **Behavior**: Schedules on first token; blocks/cancels others (e.g., via blocked tokens and ancestor cancellation).
    - **When to Use**: Racing conditions (e.g., first response wins).
    - **Scenarios**:
        - **Forks**: Only first branch proceeds.
        - **Loops**: May race iterations if concurrent.
        - **Dead Paths**: First active wins; others blocked.
        - **Example**: Waiting for fastest API response; cancel slower ones.

### Handling Common Scenarios
 
- **Simple Sequential**: Any mode schedules on token arrival (single inbound).
- **Fork-Join with Condition**: Converge blocks on false; None proceeds.
- **Looping Construct**: All modes work; Converge uses count check to avoid strictness.
- **Switch with Dangling Branches**: None ignores untaken; Converge blocks if required.
- **BPMN Alignment**: None ≈ XOR/OR-join (flexible); Converge ≈ AND-join (strict); Race ≈ Event-based; Stream ≈ partial OR.

## Consequences

- **Positive**:
    - Clearer semantics: Explicit modes reduce bugs and improve workflow design.
    - Flexibility: Users choose behavior per activity.
    - Reliability: Fixes identified flaws across forks, loops, and complexes.
    - Extensibility: Enum can grow (e.g., for BPMN Complex).

- **Negative**:
    - Complexity: More modes mean more testing; document well.
    - Performance: Token checks add minor overhead (optimize with caching).

## Alternatives Considered

- **Heuristics-Only**: Relied on inbound count/graph structure—too brittle, led to conflicts.
- **Full BPMN Gateways**: Dedicated activities per type (e.g., ParallelGateway)—overkill for Elsa's simplicity; would require major refactor.
- **Dead Path Propagation**: Emit blocked tokens on untaken paths—adds complexity; deferred for future if needed (e.g., for OR-join).
- **Counter-Based Fallback**: Retained old logic—deprecated for token purity.