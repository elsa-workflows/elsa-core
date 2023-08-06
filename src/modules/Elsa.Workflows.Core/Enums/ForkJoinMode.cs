using Elsa.Workflows.Core.Activities;

namespace Elsa.Workflows.Core;

/// <summary>
/// Controls when a <see cref="Fork"/> completes.
/// </summary>
public enum ForkJoinMode
{
    /// <summary>
    /// The <see cref="Fork"/> completes after all inbound activities have completed.
    /// </summary>
    WaitAll,
    
    /// <summary>
    /// The <see cref="Fork"/> completes as soon as any of its inbound activity completes.
    /// </summary>
    WaitAny
}