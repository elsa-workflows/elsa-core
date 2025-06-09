using Elsa.Extensions;
using Elsa.Workflows.Models;

namespace Elsa.Workflows;

/// <inheritdoc />
public class ActivityTestRunner(
    IServiceProvider serviceProvider,
    IWorkflowExecutionPipeline pipeline,
    IIdentityGenerator identityGenerator)
    : IActivityTestRunner
{
    /// <inheritdoc />
    public async Task<ActivityExecutionContext> RunAsync(WorkflowGraph workflowGraph, IActivity activity, CancellationToken cancellationToken = default)
    {
        var id = identityGenerator.GenerateId();
        var workflowExecutionContext = await WorkflowExecutionContext.CreateAsync(serviceProvider, workflowGraph, id, cancellationToken);
        
        foreach (var variable in workflowGraph.Workflow.Variables) 
            variable.Set(workflowExecutionContext.ExpressionExecutionContext!, variable.Value);
        
        workflowExecutionContext.ScheduleActivity(activity);
        workflowExecutionContext.TransitionTo(WorkflowSubStatus.Executing);

        await pipeline.ExecuteAsync(workflowExecutionContext);
        var activityExecutionContext = workflowExecutionContext
            .ActivityExecutionContexts
            .First(x => x.Activity == activity);
        return activityExecutionContext;
    }
}