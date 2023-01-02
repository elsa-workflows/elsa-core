using Elsa.Workflows.Core.Features;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Adds an extension method to the <see cref="WorkflowsFeature"/> that installs a default workflow runtime execution pipeline.
/// </summary>
public static class WorkflowsFeatureExtensions
{
    /// <summary>
    /// Installs a default workflow runtime execution pipeline.
    /// </summary>
    public static WorkflowsFeature WithDefaultRuntimeWorkflowExecutionPipeline(this WorkflowsFeature workflowsFeature) => workflowsFeature.WithWorkflowExecutionPipeline(pipeline => pipeline.UseDefaultRuntimePipeline());
}