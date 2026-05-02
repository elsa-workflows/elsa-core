namespace Elsa.Workflows.Runtime.Options;

/// <summary>
/// Controls whether an administrative pause survives a runtime generation boundary (host restart or shell reactivation).
/// </summary>
public enum PausePersistencePolicy
{
    /// <summary>
    /// Pause state is held only in memory. A fresh runtime generation always starts unpaused. Default.
    /// </summary>
    SessionScoped,

    /// <summary>
    /// Pause state is persisted via the key-value store and re-applied on the next runtime generation, so stimuli accumulated
    /// during a reload are not dispatched until an operator explicitly resumes.
    /// </summary>
    AcrossReactivations,
}
