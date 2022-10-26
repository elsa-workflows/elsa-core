using Elsa.Common.Models;
using Elsa.Jobs.Abstractions;
using Elsa.Jobs.Models;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.Jobs.Activities.Jobs;

public class ExecuteBackgroundActivityJob : Job
{
    public string WorkflowInstanceId { get; set; } = default!;
    public IActivity Activity { get; set; } = default!;
    public string BookmarkId { get; set; } = default!;
    
    protected override async ValueTask ExecuteAsync(JobExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;
        var workflowRuntime = context.GetRequiredService<IWorkflowRuntime>();
        var workflowDefinitionService = context.GetRequiredService<IWorkflowDefinitionService>();
        var workflowExecutionContextFactory = context.GetRequiredService<IWorkflowExecutionContextFactory>();
        var activityInvoker = context.GetRequiredService<IActivityInvoker>();
        var workflowState = await workflowRuntime.ExportWorkflowStateAsync(WorkflowInstanceId, cancellationToken);

        if (workflowState == null)
            throw new Exception("Workflow state not found"); 
        
        var workflowDefinition = await workflowDefinitionService.FindAsync(workflowState.DefinitionId, VersionOptions.SpecificVersion(workflowState.DefinitionVersion), cancellationToken);

        if (workflowDefinition == null)
            throw new Exception("Workflow definition not found");

        var workflow = await workflowDefinitionService.MaterializeWorkflowAsync(workflowDefinition, cancellationToken);
        var workflowExecutionContext = await workflowExecutionContextFactory.CreateAsync(workflow, workflowState.Id, workflowState, cancellationToken: cancellationToken);
        await activityInvoker.InvokeAsync(workflowExecutionContext, Activity);
    }
}