using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Workflows.CommitStates.Options;
using Elsa.Workflows.CommitStates.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.CommitStates;

public class CommitStrategiesFeature(IModule module) : FeatureBase(module)
{
    public void RegisterStrategy(IWorkflowCommitStrategy strategy)
    {
        RegisterStrategy(strategy.GetType().Name, strategy);
    }
    
    public void RegisterStrategy(string name, IWorkflowCommitStrategy strategy)
    {
        Services.Configure<CommitStateOptions>(options => options.WorkflowCommitStrategies[name] = strategy);
    }
    
    public void RegisterStrategy(IActivityCommitStrategy strategy)
    {
        RegisterStrategy(strategy.GetType().Name, strategy);
    }
    
    public void RegisterStrategy(string name, IActivityCommitStrategy strategy)
    {
        Services.Configure<CommitStateOptions>(options => options.ActivityCommitStrategies[name] = strategy);
    }
    
    public override void Apply()
    {
        Services.AddSingleton<ICommitStrategyRegistry, DefaultCommitStrategyRegistry>();
        Services.AddStartupTask<PopulateCommitStrategyRegistry>();
    }
}