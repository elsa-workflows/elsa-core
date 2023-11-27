using System.Runtime.CompilerServices;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities.Flowchart.Attributes;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Options;
using Elsa.Workflows.Runtime.Bookmarks;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Requests;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Activities;

/// <summary>
/// Creates new workflow instances of the specified workflow for each item in the data source and dispatches them for execution.
/// </summary>
[Activity("Elsa", "Composition", "Create new workflow instances for each item in the data source and dispatch them for execution.")]
[FlowNode("Finished", "Canceled", "Done")]
[UsedImplicitly]
public class BulkDispatchWorkflow : Activity
{
    private const string DispatchedInstancesCountKey = nameof(DispatchedInstancesCountKey);
    private const string FinishedInstancesCountKey = nameof(FinishedInstancesCountKey);

    /// <inheritdoc />
    public BulkDispatchWorkflow([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }

    /// <summary>
    /// The definition ID of the workflows to dispatch. 
    /// </summary>
    [Input(
        Description = "The definition ID of the workflows to dispatch.",
        UIHint = InputUIHints.WorkflowDefinitionPicker
    )]
    public Input<string> WorkflowDefinitionId { get; set; } = default!;

    /// <summary>
    /// The data source to use for dispatching the workflows.
    /// </summary>
    [Input(Description = "The data source to use for dispatching the workflows.")]
    public Input<ICollection<object>>? Items { get; set; }

    /// <summary>
    /// The data source to use for dispatching the workflows.
    /// </summary>
    [Input(Description = "The data source to use for dispatching the workflows.")]
    public IActivityDataSource? DataSource { get; set; }

    /// <summary>
    /// The correlation ID to associate the workflow with. 
    /// </summary>
    [Input(Description = "The correlation ID to associate the workflows with.")]
    public Input<string?> CorrelationId { get; set; } = default!;

    /// <summary>
    /// The input to send to the workflows.
    /// </summary>
    [Input(Description = "The input to send to the workflows.")]
    public Input<IDictionary<string, object>?> Input { get; set; } = default!;

    /// <summary>
    /// True to wait for the child workflow to complete before completing this activity, false to "fire and forget".
    /// </summary>
    [Input(Description = "Wait for the dispatched workflows to complete before completing this activity. If set, the Done outcome will be triggered instead of Finished.")]
    public Input<bool> WaitForCompletion { get; set; } = default!;

    /// <summary>
    /// An activity to execute when the child workflow finishes.
    /// </summary>
    public IActivity ChildFinished { get; set; } = default!;

    /// <summary>
    /// An activity to execute when the child workflow faults.
    /// </summary>
    public IActivity ChildFaulted { get; set; } = default!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var waitForCompletion = WaitForCompletion.GetOrDefault(context);
        var items = GetItemsAsync(context).WithCancellation(context.CancellationToken);
        var dispatchedInstancesCount = 0L;

        try
        {
            await foreach (var item in items)
            {
                await DispatchChildWorkflowAsync(context, item);
                dispatchedInstancesCount++;
            }
        }
        catch (TaskCanceledException)
        {
            await context.CompleteActivityWithOutcomesAsync("Canceled");
            return;
        }

        // If we need to wait for the child workflow to complete, create a bookmark.
        if (waitForCompletion)
        {
            var bookmarkOptions = new CreateBookmarkArgs
            {
                Callback = OnChildWorkflowCompletedAsync,
                Payload = new BulkDispatchWorkflowBookmark(dispatchedInstancesCount),
                IncludeActivityInstanceId = false
            };
            context.CreateBookmark(bookmarkOptions);
        }
        else
        {
            // Otherwise, we can complete immediately.
            await context.CompleteActivityAsync();
        }
    }

    /// <summary>
    /// Invoked when the created bookmark was persisted, which means it's safe to actually dispatch the child workflow for execution.
    /// This prevents a potential race condition where the child workflow finishes before our current workflow execution pipeline had a chance to persist its bookmarks. 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="item"></param>
    private async ValueTask<string> DispatchChildWorkflowAsync(ActivityExecutionContext context, object item)
    {
        var workflowDefinitionId = WorkflowDefinitionId.Get(context);
        var input = Input.GetOrDefault(context) ?? new Dictionary<string, object>();

        input["ParentInstanceId"] = context.WorkflowExecutionContext.Id;
        input["Item"] = item;

        var correlationId = CorrelationId.GetOrDefault(context);
        var workflowDispatcher = context.GetRequiredService<IWorkflowDispatcher>();
        var identityGenerator = context.GetRequiredService<IIdentityGenerator>();
        var instanceId = identityGenerator.GenerateId();
        var request = new DispatchWorkflowDefinitionRequest
        {
            DefinitionId = workflowDefinitionId,
            VersionOptions = VersionOptions.Published,
            Input = input,
            CorrelationId = correlationId,
            InstanceId = instanceId
        };

        await workflowDispatcher.DispatchAsync(request, context.CancellationToken);

        return instanceId;
    }

    private IAsyncEnumerable<object> GetItemsAsync(ActivityExecutionContext context)
    {
        var items = Items.Get(context);
        var dataSource = DataSource != null ? DataSource.GetDataAsync(context) : items.ToAsyncEnumerable();
        return dataSource;
    }

    private async ValueTask OnChildWorkflowCompletedAsync(ActivityExecutionContext context)
    {
        var input = context.WorkflowInput;
        var options = new ScheduleWorkOptions { Input = input };
        await context.ScheduleActivityAsync(ChildFinished, options);

        var dispatchedInstancesCount = context.GetProperty<long>(DispatchedInstancesCountKey) + 1;
        var finishedInstancesCount = context.GetProperty<long>(FinishedInstancesCountKey) + 1;

        context.SetProperty(FinishedInstancesCountKey, finishedInstancesCount);

        if (finishedInstancesCount >= dispatchedInstancesCount)
            await context.CompleteActivityAsync("Finished");
    }
}