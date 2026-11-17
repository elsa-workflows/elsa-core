namespace Elsa.Identity.Options;

/// <summary>Configures the process-local password attempt limiter.</summary>
public sealed class LocalPasswordAttemptOptions
{
    /// <summary>The duration of the failure budget window.</summary>
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>The number of failed or concurrent attempts allowed for an account per window.</summary>
    public int AccountLimit { get; set; } = 10;

    /// <summary>The number of failed or concurrent attempts allowed for a peer per window.</summary>
    public int SourceLimit { get; set; } = 100;

    /// <summary>The maximum number of distinct tracked accounts or sources per window.</summary>
    public int PartitionLimit { get; set; } = 4096;
}
