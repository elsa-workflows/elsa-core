namespace Elsa.Studio.PlatformIntegration.Contracts;

/// <summary>
/// Submits packaged workflow artifacts to Elsa Platform.
/// </summary>
public interface IPlatformArtifactSubmitClient
{
    /// <summary>
    /// Submits the specified package.
    /// </summary>
    Task<PlatformSubmitResult> SubmitAsync(PlatformWorkflowSubmitPackage package, PlatformSubmitOptions options, CancellationToken cancellationToken = default);
}
