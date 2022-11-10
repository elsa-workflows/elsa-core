using Elsa.Scheduling.Activities;

namespace Elsa.Scheduling.Models;

/// <summary>
/// Represents an execution mode for <see cref="Delay"/>. 
/// </summary>
public enum DelayBlockingStrategy
{
    /// <summary>
    /// Do not suspend the workflow, but instead wait
    /// </summary>
    NonBlocking,
    
    /// <summary>
    /// Suspend the workflow and schedule a background timer to resume the workflow.
    /// </summary>
    Blocking,
    
    /// <summary>
    /// If the delay is less than a configurable amount of time, behaves as <see cref="Blocking"/>, otherwise as <see cref="NonBlocking"/>.
    /// </summary>
    Auto
}