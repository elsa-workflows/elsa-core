namespace Elsa.Studio.Diagnostics.ConsoleLogs.Models;

/// <summary>
/// Represents a backend process, service, pod, or container source.
/// </summary>
public class ConsoleLogSource
{
    /// <summary>
    /// Gets or sets the stable source identity.
    /// </summary>
    public string Id { get; set; } = default!;

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the service name.
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Gets or sets the process ID.
    /// </summary>
    public int? ProcessId { get; set; }

    /// <summary>
    /// Gets or sets the machine name.
    /// </summary>
    public string? MachineName { get; set; }

    /// <summary>
    /// Gets or sets the pod name.
    /// </summary>
    public string? PodName { get; set; }

    /// <summary>
    /// Gets or sets the container name.
    /// </summary>
    public string? ContainerName { get; set; }

    /// <summary>
    /// Gets or sets the namespace.
    /// </summary>
    public string? Namespace { get; set; }

    /// <summary>
    /// Gets or sets the node name.
    /// </summary>
    public string? NodeName { get; set; }

    /// <summary>
    /// Gets or sets the last seen timestamp.
    /// </summary>
    public DateTimeOffset? LastSeen { get; set; }

    /// <summary>
    /// Gets or sets the source health.
    /// </summary>
    public ConsoleLogSourceHealth Health { get; set; }
}
