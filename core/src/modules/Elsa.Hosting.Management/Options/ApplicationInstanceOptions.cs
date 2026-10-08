namespace Elsa.Hosting.Management.Options;

/// <summary>
/// Options that control how the name of the current application instance is determined.
/// </summary>
/// <remarks>
/// The instance name is used to name per-instance transport entities, such as the Azure Service Bus
/// change-token subscription and queue (<c>{instanceName}-elsa-tct</c>).
/// By default a random name is generated for every process start, which means a new entity is created
/// on every restart. Under transports with a per-topic entity limit (for example Azure Service Bus,
/// which caps a topic at 2,000 subscriptions), these orphaned entities can accumulate across restarts
/// until the limit is reached and new instances can no longer start. Providing a <i>stable</i> name
/// that is reused across restarts of the same logical instance keeps the number of entities bounded.
///
/// A stable name must be both stable across restarts of the same instance and unique across instances
/// that run at the same time. In Kubernetes a StatefulSet provides exactly this (each pod keeps its
/// ordinal hostname across restarts); for a Deployment the pod name can be projected via the Downward
/// API (for example <c>metadata.name</c>).
/// </remarks>
public class ApplicationInstanceOptions
{
    /// <summary>
    /// An explicit, stable, unique-per-instance name. When set, this value is used directly and takes
    /// precedence over <see cref="InstanceNameEnvironmentVariable"/>.
    /// </summary>
    /// <remarks>
    /// Use only letters, numbers, periods, hyphens, or underscores, and start and end the value with a
    /// letter or number. Values that are too long for downstream transport entity names are shortened
    /// deterministically so the same configured value resolves to the same application instance name
    /// across restarts.
    /// </remarks>
    public string? InstanceName { get; set; }

    /// <summary>
    /// The name of an environment variable to read the instance name from when <see cref="InstanceName"/>
    /// is not set. For example, set this to <c>HOSTNAME</c> to use the Kubernetes pod name (stable across
    /// restarts when running as a StatefulSet). When <see langword="null"/> or empty, no environment
    /// variable is read and a random name is generated instead.
    /// </summary>
    /// <remarks>
    /// The environment variable name is trimmed before lookup. The value it contains follows the same
    /// character rules and deterministic shortening behavior as <see cref="InstanceName"/>.
    /// </remarks>
    public string? InstanceNameEnvironmentVariable { get; set; }
}
