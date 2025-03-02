



using Elsa.Workflows.Middleware.Activities;
// ReSharper disable once CheckNamespace
using Elsa.Workflows.Features;

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
     public static WorkflowsFeature WithDefaultWorkflowExecutionPipeline(this WorkflowsFeature workflowsFeature) => 
         workflowsFeature.WithWorkflowExecutionPipeline(pipeline => 
             pipeline.UseDefaultPipeline());

     /// <summary>
     /// Installs an activity invoker that can run activities asynchronously in the background.
     /// </summary>
     public static WorkflowsFeature WithBackgroundActivityExecutionPipeline(this WorkflowsFeature workflowsFeature) => 
          workflowsFeature.WithActivityExecutionPipeline(pipeline => 
              pipeline
                  .UseExceptionHandling()
                  .UseExecutionLogging()
                  .UseNotifications()
                  .UseBackgroundActivityInvoker());
}