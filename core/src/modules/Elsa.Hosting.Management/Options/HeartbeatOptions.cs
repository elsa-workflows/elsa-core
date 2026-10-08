namespace Elsa.Hosting.Management.Options;

/// <summary>
/// Represents the options for the application heartbeat feature.
/// </summary>
public class HeartbeatOptions
{
    /// <summary>
    /// Gets or sets the interval at which the heartbeat should be sent.
    /// </summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(1);
    
    /// <summary>
    /// Gets or sets the timeout for the heartbeat. When the heartbeat is not received within this time, the instance is considered unhealthy.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromHours(1);
}