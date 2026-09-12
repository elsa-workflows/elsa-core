namespace Elsa.Platform.Integration.Options;

public class ElsaPlatformIntegrationOptions
{
    public const string ConfigurationSection = "Elsa:PlatformIntegration";

    public bool Enabled { get; set; }

    public Uri? PlatformEndpoint { get; set; }

    public Guid WorkspaceId { get; set; }

    public Guid EngineId { get; set; }

    public string? EngineSecret { get; set; }

    public string WorkerId { get; set; } = $"{Environment.MachineName}:{Environment.ProcessId}";

    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(5);

    public TimeSpan ClaimLeaseDuration { get; set; } = TimeSpan.FromMinutes(5);

    public long MaxArtifactBytes { get; set; } = 4 * 1024 * 1024;

    public string ShellOverlayPath { get; set; } = "platform-shell-overrides.json";

    public IReadOnlyList<string> Capabilities { get; set; } = ["loom.recipe.apply"];

    public void Validate()
    {
        if (!Enabled)
            return;

        if (PlatformEndpoint is null)
            throw new InvalidOperationException("Elsa Platform endpoint is required when Platform integration is enabled.");
        if (WorkspaceId == Guid.Empty)
            throw new InvalidOperationException("Elsa Platform workspace ID is required when Platform integration is enabled.");
        if (EngineId == Guid.Empty)
            throw new InvalidOperationException("Elsa Platform engine ID is required when Platform integration is enabled.");
        if (string.IsNullOrWhiteSpace(EngineSecret))
            throw new InvalidOperationException("Elsa Platform engine secret is required when Platform integration is enabled.");
        if (string.IsNullOrWhiteSpace(WorkerId))
            throw new InvalidOperationException("Elsa Platform worker ID is required when Platform integration is enabled.");
        if (PollInterval <= TimeSpan.Zero)
            throw new InvalidOperationException("Elsa Platform poll interval must be positive.");
        if (ClaimLeaseDuration < TimeSpan.FromSeconds(1) || ClaimLeaseDuration.TotalSeconds > int.MaxValue)
            throw new InvalidOperationException("Elsa Platform claim lease duration must be between 1 second and the maximum supported Platform lease.");
        if (MaxArtifactBytes <= 0 || MaxArtifactBytes > Array.MaxLength)
            throw new InvalidOperationException("Elsa Platform maximum artifact size must be between 1 byte and the maximum runtime buffer size.");
        if (string.IsNullOrWhiteSpace(ShellOverlayPath))
            throw new InvalidOperationException("Elsa Platform shell overlay path is required when Platform integration is enabled.");
    }
}
