namespace Elsa.Connections.Features;

/// <summary>Bounds the opt-in hosted connection lifecycle reconciler.</summary>
public sealed class ConnectionLifecycleReconciliationOptions
{
    public bool Enabled { get; set; } = true;
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(1);
    public int BatchSize { get; set; } = 100;
    public int MaxConcurrency { get; set; } = 4;
    public TimeSpan RetryBackoff { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan MaxRetryBackoff { get; set; } = TimeSpan.FromMinutes(5);
}
