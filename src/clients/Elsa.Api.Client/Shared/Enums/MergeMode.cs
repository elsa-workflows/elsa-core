namespace Elsa.Api.Client.Shared.Enums;

/// <summary>
/// Specifies the strategy for handling multiple inbound execution paths in a workflow.
/// </summary>
public enum MergeMode
{
    /// <summary>
    /// Wait for all inbound paths before proceeding. 
    /// </summary>
    Converge,
    
    /// <summary>
    /// Proceed when any one inbound path completes; cancel all others.
    /// </summary>
    Race,
    
    /// <summary>
    /// Proceed when any one inbound path completes; do not cancel others.
    /// </summary>
    Stream
}