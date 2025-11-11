namespace Elsa.Api.Client.Shared.Enums;

/// <summary>
/// Specifies the strategy for handling multiple inbound execution paths in a workflow.
/// </summary>
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