using Elsa.Common;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.CommitStates.Tasks;

[UsedImplicitly]
public class PopulateCommitStrategyRegistry(ICommitStrategyRegistry registry, IOptions<CommitStateOptions> options) : IStartupTask
{
    public Task ExecuteAsync(CancellationToken cancellationToken)
    {
        foreach (var strategy in options.Value.WorkflowCommitStrategies.Values) registry.RegisterStrategy(strategy);
        foreach (var strategy in options.Value.ActivityCommitStrategies.Values) registry.RegisterStrategy(strategy);
        return Task.CompletedTask;
    }
}