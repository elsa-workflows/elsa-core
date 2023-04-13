using Elsa.Workflows.Core.Features;
using Elsa.Workflows.Core.Middleware.Activities;

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
     public static WorkflowsFeature WithDefaultRuntimeWorkflowExecutionPipeline(this WorkflowsFeature workflowsFeature) => 
         workflowsFeature.WithWorkflowExecutionPipeline(pipeline => 
             pipeline.UseDefaultRuntimePipeline());

     /// <summary>
     /// Installs an activity invoker that can run activities asynchronously in the background.
     /// </summary>
     public static WorkflowsFeature WithBackgroundActivityExecutionPipeline(this WorkflowsFeature workflowsFeature) => 
          workflowsFeature.WithActivityExecutionPipeline(pipeline => 
              pipeline
                  .UseExceptionHandling()
                  .UseExecutionLogging()
                  .UseBackgroundActivityInvoker());
}