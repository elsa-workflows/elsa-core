namespace Elsa.Studio.PlatformIntegration;

/// <summary>
/// Represents a packaged workflow submission.
/// </summary>
public sealed record PlatformWorkflowSubmitPackage(
    PlatformArtifactEnvelope Envelope,
    string WorkflowDefinitionJson,
    DateTimeOffset PackagedAt);
