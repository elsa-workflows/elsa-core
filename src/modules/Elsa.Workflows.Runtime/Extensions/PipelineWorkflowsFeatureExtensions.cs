using Elsa.Workflows.Contracts;
using Elsa.Workflows.Features;
using Elsa.Workflows.Middleware.Activities;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// Adds an extension method to the <see cref="WorkflowsFeature"/> that installs a default workflow runtime execution pipeline.
public static class PipelineWorkflowsFeatureExtensions
{
    /// Installs a default workflow runtime execution pipeline.
    public static WorkflowsFeature WithDefaultWorkflowExecutionPipeline(this WorkflowsFeature workflowsFeature, Action<IWorkflowExecutionPipelineBuilder>? configurePipeline = null)
    {
        return workflowsFeature.WithWorkflowExecutionPipeline(pipeline =>
        {
            pipeline.UseDefaultPipeline();
            configurePipeline?.Invoke(pipeline);
        });
    }

    /// Installs an activity invoker that can run activities asynchronously in the background.
    public static WorkflowsFeature WithDefaultActivityExecutionPipeline(this WorkflowsFeature workflowsFeature, Action<IActivityExecutionPipelineBuilder>? configurePipeline = null)
    {
        return workflowsFeature.WithActivityExecutionPipeline(pipeline =>
        {
            pipeline
                .UseExceptionHandling()
                .UseExecutionLogging()
                .UseNotifications()
                .UseBackgroundActivityInvoker();

            configurePipeline?.Invoke(pipeline);
        });
    }
}