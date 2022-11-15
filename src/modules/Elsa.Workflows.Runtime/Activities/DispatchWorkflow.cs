using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Runtime.Activities;

/// <summary>
/// Creates a new workflow instance of the specified workflow and dispatches it for execution.
/// </summary>
[Activity("Elsa", "Workflows", "Create a new workflow instance of the specified workflow and dispatches it for execution.")]
public class DispatchWorkflow : Activity
{
    public Input<string> WorkflowDefinitionId { get; set; } = new("");
    public Input<string> CorrelationId { get; set; } = new("");
    public Input<IDictionary<string, object>>? Input { get; set; }

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        // var workflowDefinitionId = context.Get(WorkflowDefinitionId);
        // var logger = context.GetRequiredService<ILogger<DispatchWorkflow>>();
        //
        // if (string.IsNullOrWhiteSpace(workflowDefinitionId))
        // {
        //     logger.LogWarning("No workflow definition ID specified");
        //     return;
        // }
        //
        // var workflowService = context.GetRequiredService<IWorkflowService>();
        // var input = context.Get(Input);
        // var correlationId = CorrelationId.Get(context);
        // await workflowService.DispatchWorkflowAsync(workflowDefinitionId, VersionOptions.Published, input, correlationId, context.CancellationToken);
    }
}