namespace Elsa.Workflows.Core.Activities;

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