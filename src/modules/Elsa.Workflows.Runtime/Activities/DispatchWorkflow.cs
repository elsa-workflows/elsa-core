using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Services;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime.Activities;

/// <summary>
/// Creates a new workflow instance of the specified workflow and dispatches it for execution.
/// </summary>
[Activity("Elsa", "Primitives", "Create a new workflow instance of the specified workflow and dispatch it for execution.")]
[PublicAPI]
public class DispatchWorkflow : Activity
{
    /// <summary>
    /// The definition ID of the workflow to dispatch. 
    /// </summary>
    [Input(Description = "The definition ID of the workflow to dispatch.")]
    public Input<string> WorkflowDefinitionId { get; set; } = default!;

    /// <summary>
    /// The correlation ID to associate the workflow with. 
    /// </summary>
    [Input(Description = "The correlation ID to associate the workflow with.")]
    public Input<string?> CorrelationId { get; set; } = default!;

    /// <summary>
    /// The input to send to the workflow.
    /// </summary>
    [Input(Description = "The input to send to the workflow.")]
    public Input<IDictionary<string, object>?> Input { get; set; } = default!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var workflowDefinitionId = WorkflowDefinitionId.Get(context);
        var input = Input.TryGet(context);
        var correlationId = CorrelationId.TryGet(context);
        var workflowDispatcher = context.GetRequiredService<IWorkflowDispatcher>();
        var request = new DispatchWorkflowDefinitionRequest(workflowDefinitionId, VersionOptions.Published, input, correlationId);
        await workflowDispatcher.DispatchAsync(request, context.CancellationToken);
    }
}