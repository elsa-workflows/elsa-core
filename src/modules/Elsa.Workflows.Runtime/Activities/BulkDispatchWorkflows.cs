using System.Runtime.CompilerServices;
using Elsa.Common.Models;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Workflows.Activities.Flowchart.Attributes;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.UIHints;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Elsa.Workflows.Runtime.Bookmarks;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Services;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Activities;

/// <summary>
/// Creates new workflow instances of the specified workflow for each item in the data source and dispatches them for execution.
/// </summary>
[Activity("Elsa", "Composition", "Create new workflow instances for each item in the data source and dispatch them for execution.", Kind = ActivityKind.Task)]
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
    [Input(Description = """Additional input to send to the workflows being dispatched. The "Item" key is reserved and should not be used.""")]
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
    [Port]
    public IActivity? ChildFinished { get; set; }

    /// <summary>
    /// An activity to execute when the child workflow faults.
    /// </summary>
    [Port]
    public IActivity? ChildFaulted { get; set; }

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var waitForCompletion = WaitForCompletion.GetOrDefault(context);
        var items = GetItemsAsync(context).WithCancellation(context.CancellationToken);
        var dispatchedInstancesCount = 0L;
        var batchSize = 1000;
        var batch = new List<object>();

        await foreach (var item in items)
        {
            if (context.WorkflowExecutionContext.CancellationTokens.ApplicationCancellationToken
                .IsCancellationRequested)
                break;
            
            batch.Add(item);

            if (batch.Count < batchSize)
                continue;

            await ProcessBatch(context, batch);
            dispatchedInstancesCount += batch.Count;
            batch.Clear();
        }

        // Process the last batch if it has any items.
        if (batch.Count > 0)
        {
            await ProcessBatch(context, batch);
            dispatchedInstancesCount += batch.Count;
        }

        context.SetProperty(DispatchedInstancesCountKey, dispatchedInstancesCount);

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
                IncludeActivityInstanceId = false,
                AutoBurn = false,
            };
            context.CreateBookmark(bookmarkOptions);
        }
        else
        {
            // Otherwise, we can complete immediately.
            await context.CompleteActivityAsync();
        }

        // Cancelling children
    }

    private async Task ProcessBatch(ActivityExecutionContext context, List<object> items)
    {
        var tasks = items.Select(async item =>
        {
            try
            {
                await DispatchChildWorkflowAsync(context, item);
            }
            catch (TaskCanceledException)
            {
                await context.CompleteActivityWithOutcomesAsync("Canceled");
            }
            catch (Exception ex)
            {
                context.JournalData.Add("Error", ex.Message);
            }
        });

        await Task.WhenAll(tasks);
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

        await workflowDispatcher.DispatchAsync(request, cancellationToken: context.CancellationToken);

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
        var workflowInstanceId = input["WorkflowInstanceId"].ConvertTo<string>()!;
        var workflowSubStatus = input["WorkflowSubStatus"].ConvertTo<WorkflowSubStatus>();
        var workflowOutput = input["WorkflowOutput"].ConvertTo<IDictionary<string, object>>();
        var finishedInstancesCount = context.GetProperty<long>(FinishedInstancesCountKey) + 1;

        context.SetProperty(FinishedInstancesCountKey, finishedInstancesCount);

        var childInstanceId = new Variable<string>("ChildInstanceId", workflowInstanceId)
        {
            StorageDriverType = typeof(WorkflowStorageDriver)
        };

        var variables = new List<Variable>
        {
            childInstanceId
        };

        var options = new ScheduleWorkOptions
        {
            Input = input,
            Variables = variables,
            CompletionCallback = OnChildFinishedCompletedAsync
        };

        switch (workflowSubStatus)
        {
            case WorkflowSubStatus.Faulted when ChildFaulted is not null:
                await context.ScheduleActivityAsync(ChildFaulted, options);
                return;
            case WorkflowSubStatus.Finished when ChildFinished is not null:
                await context.ScheduleActivityAsync(ChildFinished, options);
                return;
            default:
                await CheckIfFinishedAsync(context);
                break;
        }
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