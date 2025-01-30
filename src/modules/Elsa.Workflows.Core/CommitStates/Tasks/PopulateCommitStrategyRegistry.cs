using Elsa.Common;
using Elsa.Workflows.CommitStates.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.CommitStates.Tasks;

public class PopulateCommitStrategyRegistry(ICommitStrategyRegistry registry, IOptions<CommitStateOptions> options) : IStartupTask
{
    public Task ExecuteAsync(CancellationToken cancellationToken)
    {
        foreach (var strategy in options.Value.WorkflowCommitStrategies) registry.RegisterStrategy(strategy.Key, strategy.Value);
        foreach (var strategy in options.Value.ActivityCommitStrategies) registry.RegisterStrategy(strategy.Key, strategy.Value);
        return Task.CompletedTask;
    }
}