using Elsa.Workflows.Runtime.Services;

namespace Elsa.Workflows.Runtime.Options;

/// <summary>
/// Provides options related to distributed locking, which is used by <see cref="DefaultWorkflowRuntime"/>.
/// </summary>
public class DistributedLockingOptions
{
    /// <summary>
    /// The maximum amount of time to wait before giving up trying to acquire a lock. Defaults to 10 minutes.
    /// </summary>
    public TimeSpan LockAcquisitionTimeout { get; set; } = TimeSpan.FromMinutes(10);
}