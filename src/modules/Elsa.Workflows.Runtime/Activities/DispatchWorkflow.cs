using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Runtime.Bookmarks;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Requests;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Activities;

/// <summary>
/// Creates a new workflow instance of the specified workflow and dispatches it for execution.
/// </summary>
[Activity("Elsa", "Composition", "Create a new workflow instance of the specified workflow and dispatch it for execution.")]
[PublicAPI]
public class DispatchWorkflow : Activity<object>, IBookmarksPersistedHandler
{
    /// <summary>
    /// The definition ID of the workflow to dispatch. 
    /// </summary>
    [Input(
        Description = "The definition ID of the workflow to dispatch.",
        UIHint = InputUIHints.WorkflowDefinitionPicker
    )]
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

    /// <summary>
    /// True to wait for the child workflow to complete before completing this activity, false to "fire and forget".
    /// </summary>
    [Input(Description = "Wait for the child workflow to complete before completing this activity.")]
    public Input<bool> WaitForCompletion { get; set; } = default!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var waitForCompletion = WaitForCompletion.GetOrDefault(context);
        var identityGenerator = context.GetRequiredService<IIdentityGenerator>();
        var instanceId = identityGenerator.GenerateId();
        context.TransientProperties["ChildInstanceId"] = instanceId;

        // If we need to wait for the child workflow to complete, create a bookmark.
        if (waitForCompletion)
        {
            context.CreateBookmark(new DispatchWorkflowBookmark(instanceId), OnChildWorkflowCompletedAsync);
        }
        else
        {
            // Otherwise, we can complete.
            await BookmarksPersistedAsync(context);
            await context.CompleteActivityAsync();
        }
    }

    /// <summary>
    /// Invoked when the created bookmark was persisted, which means it's safe to actually dispatch the child workflow for execution.
    /// This prevents a potential race condition where the child workflow finishes before our current workflow execution pipeline had a chance to persist its bookmarks. 
    /// </summary>
    /// <param name="context"></param>
    public async ValueTask BookmarksPersistedAsync(ActivityExecutionContext context)
    {
        var workflowDefinitionId = WorkflowDefinitionId.Get(context);
        var input = Input.GetOrDefault(context) ?? new Dictionary<string, object>();

        input["ParentInstanceId"] = context.WorkflowExecutionContext.Id;
        
        var correlationId = CorrelationId.GetOrDefault(context);
        var workflowDispatcher = context.GetRequiredService<IWorkflowDispatcher>();
        var instanceId = (string)context.TransientProperties["ChildInstanceId"];
        var request = new DispatchWorkflowDefinitionRequest
        {
            DefinitionId = workflowDefinitionId,
            VersionOptions = VersionOptions.Published,
            Input = input,
            CorrelationId = correlationId,
            InstanceId = instanceId
        };
        
        // Dispatch the child workflow.
        await workflowDispatcher.DispatchAsync(request, context.CancellationToken);
    }
    
    private async ValueTask OnChildWorkflowCompletedAsync(ActivityExecutionContext context)
    {
        var input = context.Input;
        context.Set(Result, input);
        await context.CompleteActivityAsync();
    }
}