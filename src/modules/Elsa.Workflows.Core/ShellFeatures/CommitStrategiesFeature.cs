using CShells.Features;
using Elsa.Extensions;
using Elsa.Workflows.CommitStates;
using Elsa.Workflows.CommitStates.Tasks;
using Elsa.Platform.PackageManifest.Generator.Hints;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ShellFeatures;

[ManifestFeatureCategory(ManifestFeatureCategories.Workflows)]
[ShellFeature(
    "CommitStrategies",
    DisplayName = "Commit Strategies",
    Description = "Registers workflow commit strategies")]
public class CommitStrategiesFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ICommitStrategyRegistry, DefaultCommitStrategyRegistry>();
        services.AddStartupTask<PopulateCommitStrategyRegistry>();
    }
}
