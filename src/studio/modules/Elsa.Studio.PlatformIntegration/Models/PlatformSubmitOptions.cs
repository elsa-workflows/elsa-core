namespace Elsa.Studio.PlatformIntegration;

/// <summary>
/// Configures Elsa Platform artifact submission from Studio.
/// </summary>
public sealed record PlatformSubmitOptions
{
    /// <summary>
    /// Whether Platform submission is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Whether workflow publish notifications should submit workflow artifacts.
    /// </summary>
    public bool SubmitOnWorkflowPublished { get; set; } = true;

    /// <summary>
    /// The Elsa Platform endpoint.
    /// </summary>
    public Uri? PlatformEndpoint { get; set; }

    /// <summary>
    /// The Platform workspace receiving submitted artifacts.
    /// </summary>
    public Guid? WorkspaceId { get; set; }

    /// <summary>
    /// The Platform application receiving the desired-state revision.
    /// </summary>
    public Guid? ApplicationId { get; set; }

    /// <summary>
    /// The Platform environment receiving the desired-state revision.
    /// </summary>
    public Guid? EnvironmentId { get; set; }

    /// <summary>
    /// Optional revision label. When not set, Studio generates a workflow-specific label.
    /// </summary>
    public string? RevisionLabel { get; set; }

    /// <summary>
    /// Optional revision commit identifier.
    /// </summary>
    public string? RevisionCommit { get; set; }

    /// <summary>
    /// Optional request customization for authentication headers.
    /// </summary>
    public Func<HttpRequestMessage, CancellationToken, Task>? ConfigureRequestAsync { get; set; }

    /// <summary>
    /// The Studio producer name to record in Platform artifact metadata.
    /// </summary>
    public string ProducerName { get; set; } = "Elsa Studio";

    /// <summary>
    /// The Studio producer version to record in Platform artifact metadata.
    /// </summary>
    public string? ProducerVersion { get; set; }

    /// <summary>
    /// The provider responsible for serving artifact payloads.
    /// </summary>
    public string PayloadProvider { get; set; } = "producer-managed";

    /// <summary>
    /// The URI scheme used for producer-managed payload references.
    /// </summary>
    public string PayloadUriScheme { get; set; } = "studio";

    /// <summary>
    /// The workflow artifact schema version.
    /// </summary>
    public string ArtifactSchemaVersion { get; set; } = PlatformArtifactEnvelopeConstants.DefaultArtifactSchemaVersion;

    /// <summary>
    /// Optional workflow runtime version range expected by this artifact.
    /// </summary>
    public string? RuntimeVersionRange { get; set; }

    /// <summary>
    /// Required runtime capabilities for this artifact.
    /// </summary>
    public IReadOnlyList<string> RequiredCapabilities { get; set; } = ["loom.recipe.apply"];

    /// <summary>
    /// Whether required Platform submission settings have been provided.
    /// </summary>
    public bool IsConfigured =>
        PlatformEndpoint is not null
        && WorkspaceId is { } workspaceId && workspaceId != Guid.Empty
        && ApplicationId is { } applicationId && applicationId != Guid.Empty
        && EnvironmentId is { } environmentId && environmentId != Guid.Empty;
}
