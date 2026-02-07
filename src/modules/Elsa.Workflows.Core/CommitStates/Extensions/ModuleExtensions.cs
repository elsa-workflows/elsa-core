using Elsa.Workflows.CommitStates;
using Elsa.Workflows.Features;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class WorkflowsFeatureCommitStateExtensions
{
    public static WorkflowsFeature UseCommitStrategies(this WorkflowsFeature workflowsFeature, Action<CommitStrategiesFeature>? configure = null)
    {
        workflowsFeature.Module.Use(configure);
        return workflowsFeature;
    }
    
}