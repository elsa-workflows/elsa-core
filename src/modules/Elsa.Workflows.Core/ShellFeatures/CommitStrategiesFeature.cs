using CShells.Features;
using Elsa.Extensions;
using Elsa.Workflows.CommitStates;
using Elsa.Workflows.CommitStates.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ShellFeatures;

[ShellFeature("CommitStrategies")]
public class CommitStrategiesFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ICommitStrategyRegistry, DefaultCommitStrategyRegistry>();
        services.AddStartupTask<PopulateCommitStrategyRegistry>();
    }
}