using Elsa.Platform.Integration.Models;

namespace Elsa.Platform.Integration.Services;

public interface IPlatformRecipeArtifactApplier
{
    Task<PlatformRecipeArtifactApplyResult> ApplyAsync(
        PlatformRuntimeCommand command,
        PlatformArtifactItem artifact,
        Stream artifactZip,
        CancellationToken cancellationToken = default);
}

public sealed record PlatformRecipeArtifactApplyResult(
    PlatformArtifactStatus Status,
    PlatformArtifactDigest? ObservedDigest,
    string? RuntimeReference,
    IReadOnlyList<PlatformDiagnostic> Diagnostics)
{
    public bool Succeeded => Status == PlatformArtifactStatus.Applied;
}
