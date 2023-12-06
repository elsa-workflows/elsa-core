using System.Runtime.CompilerServices;
using Elsa.Common.Models;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Expressions.Options;
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
public class BulkDispatchWorkflows : Activity
{
    private const string DispatchedInstancesCountKey = nameof(DispatchedInstancesCountKey);
    private const string FinishedInstancesCountKey = nameof(FinishedInstancesCountKey);

    /// <inheritdoc />
    public BulkDispatchWorkflows([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
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
    [Input(
        DisplayName = "Correlation ID Function",
        Description = "A function to compute the correlation ID to associate a dispatched workflow with. Receives the current item as an argument called Item.",
        AutoEvaluate = false)]
    public Input<string?>? CorrelationIdFunction { get; set; }

    /// <summary>
    /// The input to send to the workflows.
    /// </summary>
    [Input(Description = "The input to send to the workflows.")]
    public Input<IDictionary<string, object>?> Input { get; set; } = default!;

    /// <summary>
    /// True to wait for the child workflow to complete before completing this activity, false to "fire and forget".
    /// </summary>
    [Input(
        Description = "Wait for the dispatched workflows to complete before completing this activity. If set, the Finished outcome will not trigger.",
        DefaultValue = true)]
    public Input<bool> WaitForCompletion { get; set; } = new(true);

    /// <summary>
    /// An activity to execute when the child workflow finishes.
    /// </summary>
    public IActivity? ChildFinished { get; set; } = default!;

    /// <summary>
    /// An activity to execute when the child workflow faults.
    /// </summary>
    public IActivity? ChildFaulted { get; set; } = default!;

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
            var workflowInstanceId = context.WorkflowExecutionContext.Id;
            var bookmarkOptions = new CreateBookmarkArgs
            {
                Callback = OnChildWorkflowCompletedAsync,
                Payload = new BulkDispatchWorkflowsBookmark(workflowInstanceId)
                {
                    ScheduledInstanceIdsCount = dispatchedInstancesCount
                },
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

    private async ValueTask<string> DispatchChildWorkflowAsync(ActivityExecutionContext context, object item)
    {
        var workflowDefinitionId = WorkflowDefinitionId.Get(context);
        var parentInstanceId = context.WorkflowExecutionContext.Id;
        var input = Input.GetOrDefault(context) ?? new Dictionary<string, object>();
        var properties = new Dictionary<string, object>
        {
            ["ParentInstanceId"] = parentInstanceId
        };
        var evaluatorOptions = new ExpressionEvaluatorOptions
        {
            Arguments = new Dictionary<string, object>
            {
                ["Item"] = item
            }
        };

        input["ParentInstanceId"] = parentInstanceId;
        input["Item"] = item;

        var workflowDispatcher = context.GetRequiredService<IWorkflowDispatcher>();
        var identityGenerator = context.GetRequiredService<IIdentityGenerator>();
        var instanceId = identityGenerator.GenerateId();
        var evaluator = context.GetRequiredService<IExpressionEvaluator>();
        var correlationId = CorrelationIdFunction != null ? await evaluator.EvaluateAsync<string>(CorrelationIdFunction!, context.ExpressionExecutionContext, evaluatorOptions) : null;
        var request = new DispatchWorkflowDefinitionRequest
        {
            DefinitionId = workflowDefinitionId,
            VersionOptions = VersionOptions.Published,
            Input = input,
            Properties = properties,
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
        var options = new ScheduleWorkOptions { Input = input, CompletionCallback = OnChildFinishedCompletedAsync };
        var finishedInstancesCount = context.GetProperty<long>(FinishedInstancesCountKey) + 1;

        context.SetProperty(FinishedInstancesCountKey, finishedInstancesCount);

        if (ChildFinished is not null)
        {
            await context.ScheduleActivityAsync(ChildFinished, options);
            return;
        }

        await CheckIfFinishedAsync(context);
    }

    private async ValueTask OnChildFinishedCompletedAsync(ActivityCompletedContext context)
    {
        await CheckIfFinishedAsync(context.TargetContext);
    }

    private async ValueTask CheckIfFinishedAsync(ActivityExecutionContext context)
    {
        var dispatchedInstancesCount = context.GetProperty<long>(DispatchedInstancesCountKey);
        var finishedInstancesCount = context.GetProperty<long>(FinishedInstancesCountKey);

        if (finishedInstancesCount >= dispatchedInstancesCount)
            await context.CompleteActivityWithOutcomesAsync("Finished", "Done");
    }
}