namespace Elsa.Workflows.Activities.Flowchart.Models;

/// <summary>
/// Specifies the strategy for handling multiple inbound execution paths in a workflow.
/// Uses flow-based terminology to describe merge behavior.
/// </summary>
public enum MergeMode
{
    /// <summary>
    /// Flows freely when possible, ignoring dead/untaken paths.
    /// Opportunistic execution based on upstream completion.
    /// Uses approximation that proceeds after all upstream sources complete.
    /// Suitable for flexible, unstructured merges where optional branches shouldn't block.
    /// </summary>
    Stream,

    /// <summary>
    /// Waits for all forward (acyclic) inbound connections before proceeding.
    /// Unlike Converge, this ignores backward connections (loops).
    /// Will block on dead/untaken paths if they are forward connections.
    /// Use for synchronization points in structured fork-join patterns.
    /// </summary>
    Merge,

    /// <summary>
    /// Converges all inbound paths, requiring every connection to execute.
    /// Blocks until all branches complete, including unactivated ones.
    /// Strictest mode - will block on dead/untaken paths.
    /// Use when every single inbound path must execute before proceeding.
    /// </summary>
    Converge,

    /// <summary>
    /// Cascades execution for each arriving token independently.
    /// Allows multiple concurrent executions (one per arriving token).
    /// Use for streaming scenarios where each branch should trigger separate processing.
    /// </summary>
    Cascade,

    /// <summary>
    /// Races inbound branches, executing on first arrival and blocking others.
    /// Schedule on the first arriving token, block or cancel others.
    /// Use for competitive scenarios where only the first result matters.
    /// </summary>
    Race
}