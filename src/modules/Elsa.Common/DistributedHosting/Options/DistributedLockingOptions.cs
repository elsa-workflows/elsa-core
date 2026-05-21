using JetBrains.Annotations;

namespace Elsa.Common.DistributedHosting;

/// <summary>
/// Provides options related to distributed locking, which is used by the workflow runtime.
/// </summary>
[UsedImplicitly]
public class DistributedLockingOptions
{
    /// <summary>
    /// The maximum amount of time to wait before giving up trying to acquire a lock. Defaults to 10 minutes.
    /// </summary>
    public TimeSpan LockAcquisitionTimeout { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Allows the distributed workflow runtime to use a lock provider that only coordinates within this node or local file system.
    /// Enable this only for single-host development or tests. Clustered deployments should configure a cross-node provider such as Redis, SQL Server, or PostgreSQL.
    /// </summary>
    public bool AllowLocalLockProviderInDistributedRuntime { get; set; }
}
