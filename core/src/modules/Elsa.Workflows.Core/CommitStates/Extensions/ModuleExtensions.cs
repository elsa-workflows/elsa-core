using Elsa.Workflows.CommitStates;
using Elsa.Workflows.Features;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extension methods for configuring commit strategies on <see cref="WorkflowsFeature"/>.
/// </summary>
public static class WorkflowsFeatureCommitStateExtensions
{
    /// <summary>
    /// Configures commit strategies for workflows.
    /// </summary>
    /// <param name="workflowsFeature">The workflows feature.</param>
    /// <param name="configure">An optional configuration delegate for the commit strategies feature.</param>
    /// <returns>The workflows feature for chaining.</returns>
    public static WorkflowsFeature UseCommitStrategies(this WorkflowsFeature workflowsFeature, Action<CommitStrategiesFeature>? configure = null)
    {
        workflowsFeature.Module.Use(configure);
        return workflowsFeature;
    }

    /// <summary>
    /// Sets the specified workflow commit strategy as the global default for all workflows that do not specify their own strategy.
    /// The strategy will not be added to the registry and serves only as a fallback.
    /// </summary>
    /// <param name="workflowsFeature">The workflows feature.</param>
    /// <param name="strategy">The workflow commit strategy instance to use as the default.</param>
    /// <returns>The workflows feature for chaining.</returns>
    public static WorkflowsFeature WithDefaultWorkflowCommitStrategy(this WorkflowsFeature workflowsFeature, IWorkflowCommitStrategy strategy)
    {
        workflowsFeature.Module.Use<CommitStrategiesFeature>(feature => feature.SetDefaultWorkflowCommitStrategy(strategy));
        return workflowsFeature;
    }

    /// <summary>
    /// Sets the specified activity commit strategy as the global default for all activities that do not specify their own strategy.
    /// The strategy will not be added to the registry and serves only as a fallback.
    /// </summary>
    /// <param name="workflowsFeature">The workflows feature.</param>
    /// <param name="strategy">The activity commit strategy instance to use as the default.</param>
    /// <returns>The workflows feature for chaining.</returns>
    public static WorkflowsFeature WithDefaultActivityCommitStrategy(this WorkflowsFeature workflowsFeature, IActivityCommitStrategy strategy)
    {
        workflowsFeature.Module.Use<CommitStrategiesFeature>(feature => feature.SetDefaultActivityCommitStrategy(strategy));
        return workflowsFeature;
    }
}