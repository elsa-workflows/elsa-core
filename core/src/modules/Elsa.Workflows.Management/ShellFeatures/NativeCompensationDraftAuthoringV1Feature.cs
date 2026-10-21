using CShells.Features;
using Elsa.Platform.PackageManifest.Generator.Hints;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa;

/// <summary>
/// Advertises support for effect-free native compensation draft authoring.
/// </summary>
[ManifestFeatureCategory("Workflows")]
[ShellFeature(
    DisplayName = "Native Compensation Draft Authoring v1",
    Description = "Supports effect-free authoring and storage of native compensation drafts")]
[UsedImplicitly]
public sealed class NativeCompensationDraftAuthoringV1Feature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
    }
}
