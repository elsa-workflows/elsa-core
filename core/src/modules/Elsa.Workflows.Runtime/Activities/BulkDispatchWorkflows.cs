using System.Runtime.CompilerServices;
using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Workflows.Activities.Flowchart.Attributes;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Stimuli;
using Elsa.Workflows.Runtime.UIHints;
using Elsa.Workflows.Signals;
using Elsa.Workflows.State;
using Elsa.Workflows.UIHints;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime.Activities;

/// <summary>
/// Creates new workflow instances of the specified workflow for each item in the data source and dispatches them for execution.
/// When <see cref="WaitForCompletion"/> is enabled and the activity is cancelled while it waits, the child workflows it is waiting for
/// are cancelled too. This only reaches children that have made their first commit: until then a new child exists only in memory and
/// is invisible to the workflow instance store. Children are missed when the activity is cancelled in the burst that dispatched them,
/// while the dispatch queue or message transport is backlogged, or while a child's first burst is still running. Children dispatched by a
/// version without this behavior do not carry the dispatching activity instance ID, so cancelling an activity that was already waiting
/// for them when this shipped leaves them running.
/// </summary>
[Activity("Elsa", "Composition", "Create new workflow instances for each item in the data source and dispatch them for execution.", Kind = ActivityKind.Task)]
[FlowNode("Completed", "Canceled", "Done")]
[UsedImplicitly]
public class BulkDispatchWorkflows : Activity
{
    private const string DispatchedInstancesCountKey = nameof(DispatchedInstancesCountKey);
    private const string CompletedInstancesCountKey = nameof(CompletedInstancesCountKey);
    private const string WaitsForChildrenKey = "WaitsForChildren";
    private const int ChildPageSize = 100;
    private const int MaxConsecutiveReadFailures = 3;

    /// <inheritdoc />
    public BulkDispatchWorkflows([CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : base(source, line)
    {
        OnSignalReceived<CancelSignal>(OnCancelledAsync);
    }

    /// <summary>
    /// The definition ID of the workflows to dispatch. 
    /// </summary>
    [Input(
        DisplayName = "Workflow Definition",
        Description = "The definition ID of the workflows to dispatch.",
        UIHint = InputUIHints.WorkflowDefinitionPicker
    )]
    public Input<string> WorkflowDefinitionId { get; set; } = null!;

    /// <summary>
    /// The data source to use for dispatching the workflows.
    /// </summary>
    [Input(Description = "The data source to use for dispatching the workflows.")]
    public Input<object> Items { get; set; } = null!;

    /// <summary>
    /// The default key to use for the item input. Will not be used if the Items contain a list of dictionaries.
    /// </summary>
    [Input(Description = "The default key to use for the input name when sending the current item to the dispatched workflow. Will not be used if the Items field contain a list of dictionaries", DefaultValue = "Item")]
    public Input<string> DefaultItemInputKey { get; set; } = new("Item");

    /// <summary>
    /// The correlation ID to associate the workflow with. 
    /// </summary>
    [Input(
        DisplayName = "Correlation ID Function",
        Description = "A function to compute the correlation ID to associate a dispatched workflow with.",
        AutoEvaluate = false)]
    public Input<string?>? CorrelationIdFunction { get; set; }

    /// <summary>
    /// The input to send to the workflows.
    /// </summary>
    [Input(Description = "Additional input to send to the workflows being dispatched.")]
    public Input<IDictionary<string, object>?> Input { get; set; } = null!;

    /// <summary>
    /// True to wait for the child workflow to complete before completing this activity, false to "fire and forget".
    /// </summary>
    [Input(
        Description = "Wait for the dispatched workflows to complete before completing this activity.",
        DefaultValue = true)]
    public Input<bool> WaitForCompletion { get; set; } = new(true);

    /// <summary>
    /// Indicates whether a new trace context should be started for the workflow execution.
    /// </summary>
    [Input(Description = "Start a new trace context when using Open Telemetry.", Category = "Open Telemetry")]
    public Input<bool> StartNewTrace { get; set; } = new(false);

    /// <summary>
    /// The channel to dispatch the workflow to.
    /// </summary>
    [Input(
        DisplayName = "Channel",
        Description = "The channel to dispatch the workflow to.",
        UIHint = InputUIHints.DropDown,
        UIHandler = typeof(DispatcherChannelOptionsProvider)
    )]
    public Input<string?> ChannelName { get; set; } = null!;

    /// <summary>
    /// An activity to execute when the child workflow finishes.
    /// </summary>
    [Port] public IActivity? ChildCompleted { get; set; }

    /// <summary>
    /// An activity to execute when the child workflow faults.
    /// </summary>
    [Port] public IActivity? ChildFaulted { get; set; }

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var waitForCompletion = WaitForCompletion.GetOrDefault(context);
        var startNewTrace = StartNewTrace.GetOrDefault(context);
        var items = await context.GetItemSource<object>(Items).ToListAsync(context.CancellationToken);
        var count = items.Count;

        // Dispatch the child workflows.
        foreach (var item in items)
            await DispatchChildWorkflowAsync(context, item, waitForCompletion, startNewTrace, count);

        // Store the number of dispatched instances for tracking.
        context.SetProperty(DispatchedInstancesCountKey, count);

        // If we need to wait for the child workflows to complete (if any), create a bookmark.
        if (waitForCompletion && count > 0)
        {
            // Remember that this activity waits for its children, so cancelling it can cancel them.
            context.SetProperty(WaitsForChildrenKey, true);

            var workflowInstanceId = context.WorkflowExecutionContext.Id;
            var bookmarkOptions = new CreateBookmarkArgs
            {
                Callback = OnChildWorkflowCompletedAsync,
                Stimulus = new BulkDispatchWorkflowsStimulus(workflowInstanceId)
                {
                    ParentInstanceId = context.WorkflowExecutionContext.Id,
                    ScheduledInstanceIdsCount = count
                },
                IncludeActivityInstanceId = false,
                AutoBurn = false,
            };

            context.CreateBookmark(bookmarkOptions);
        }
        else
        {
            // Otherwise, we can complete immediately.
            await context.CompleteActivityWithOutcomesAsync("Done");
        }
    }

    private async ValueTask<string> DispatchChildWorkflowAsync(ActivityExecutionContext context, object item, bool waitForCompletion, bool startNewTrace, int scheduledInstancesCount)
    {
        var workflowDefinitionId = WorkflowDefinitionId.Get(context);
        var workflowDefinitionService = context.GetRequiredService<IWorkflowDefinitionService>();
        var workflowGraph = await workflowDefinitionService.FindWorkflowGraphAsync(workflowDefinitionId, VersionOptions.Published);

        if (workflowGraph == null)
            throw new($"No published version of workflow definition with ID {workflowDefinitionId} found.");

        var parentInstanceId = context.WorkflowExecutionContext.Id;
        var baseInput = Input.GetOrDefault(context);
        var input = baseInput != null ? new Dictionary<string, object>(baseInput) : new Dictionary<string, object>();
        var channelName = ChannelName.GetOrDefault(context);
        var defaultInputItemKey = DefaultItemInputKey.GetOrDefault(context, () => "Item")!;
        var properties = new Dictionary<string, object>
        {
            ["ParentInstanceId"] = parentInstanceId
        };

        if (waitForCompletion) properties[WorkflowDispatchProperties.WaitForCompletion] = true;
        if (startNewTrace) properties["StartNewTrace"] = true;

        if (waitForCompletion)
        {
            // Children carry the waiting activity instance id rather than this activity storing their IDs, so a large
            // batch does not grow the activity's state.
            properties[WorkflowDispatchProperties.WaitingActivityInstanceId] = context.Id;

            var activityTypeName = ActivityTypeNameHelper.GenerateTypeName<BulkDispatchWorkflows>();
            var stimulus = new BulkDispatchWorkflowsStimulus(parentInstanceId)
            {
                ScheduledInstanceIdsCount = scheduledInstancesCount
            };
            var stimulusHash = context.GetRequiredService<IStimulusHasher>().Hash(activityTypeName, stimulus);
            DispatchWorkflowActivationDeniedRoute.Add(properties, activityTypeName, stimulusHash);
        }

        var itemDictionary = new Dictionary<string, object>
        {
            [defaultInputItemKey] = item
        };

        var evaluatorOptions = new ExpressionEvaluatorOptions
        {
            Arguments = itemDictionary
        };

        var inputDictionary = item as IDictionary<string, object> ?? itemDictionary;
        input["ParentInstanceId"] = parentInstanceId;
        input.Merge(inputDictionary);

        var workflowDispatcher = context.GetRequiredService<IWorkflowDispatcher>();
        var identityGenerator = context.GetRequiredService<IIdentityGenerator>();
        var evaluator = context.GetRequiredService<IExpressionEvaluator>();
        var correlationId = CorrelationIdFunction != null ? await evaluator.EvaluateAsync<string>(CorrelationIdFunction!, context.ExpressionExecutionContext, evaluatorOptions) : null;
        var instanceId = identityGenerator.GenerateId();
        var request = new DispatchWorkflowDefinitionRequest(workflowGraph.Workflow.Identity.Id)
        {
            ParentWorkflowInstanceId = parentInstanceId,
            Input = input,
            Properties = properties,
            CorrelationId = correlationId,
            InstanceId = instanceId
        };
        var options = new DispatchWorkflowOptions
        {
            Channel = channelName
        };

        await workflowDispatcher.DispatchAsync(request, options, context.CancellationToken);
        return instanceId;
    }

    private async ValueTask OnChildWorkflowCompletedAsync(ActivityExecutionContext context)
    {
        var input = context.WorkflowInput;
        var workflowInstanceId = input["WorkflowInstanceId"].ConvertTo<string>()!;
        var workflowSubStatus = input["WorkflowSubStatus"].ConvertTo<WorkflowSubStatus>();
        var cannotStart = input.TryGetValue("CannotStart", out var cannotStartValue) && cannotStartValue is true;
        var finishedInstancesCount = context.GetProperty<long>(CompletedInstancesCountKey) + 1;

        context.SetProperty(CompletedInstancesCountKey, finishedInstancesCount);

        var variables = new List<Variable>();
        if (!cannotStart)
        {
            variables.Add(new Variable<string>("ChildInstanceId", workflowInstanceId)
            {
                StorageDriverType = typeof(WorkflowInstanceStorageDriver)
            });
        }

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
            case WorkflowSubStatus.Finished when ChildCompleted is not null:
                await context.ScheduleActivityAsync(ChildCompleted, options);
                return;
            default:
                await AttemptToCompleteAsync(context);
                break;
        }
    }

    private async ValueTask OnChildFinishedCompletedAsync(ActivityCompletedContext context)
    {
        await AttemptToCompleteAsync(context.TargetContext);
    }

    private async ValueTask AttemptToCompleteAsync(ActivityExecutionContext context)
    {
        var dispatchedInstancesCount = context.GetProperty<long>(DispatchedInstancesCountKey);
        var finishedInstancesCount = context.GetProperty<long>(CompletedInstancesCountKey);

        if (finishedInstancesCount >= dispatchedInstancesCount)
            await context.CompleteActivityWithOutcomesAsync("Completed", "Done");
    }

    /// <summary>
    /// Cancels the running children this activity instance was waiting for. They are found by the parent instance ID and
    /// then matched on the activity instance ID they carry, so fire-and-forget children and the children of another
    /// dispatching activity of the same parent are left alone. Children that already finished are left alone too,
    /// together with anything they dispatched. Children dispatched before the parent commits are not in the store yet
    /// and are not covered. The parent's children are read one page at a time, and each page's matches are cancelled
    /// before the next page is read. Only a successfully read short page ends the loop. A failed read is retried once
    /// before that page is skipped and the next page is still attempted. After
    /// <see cref="MaxConsecutiveReadFailures"/> consecutive skipped pages, paging gives up. A failed cancel of a page
    /// that was read is logged and paging continues. This best-effort cleanup does not fault the parent's cancel path.
    /// </summary>
    private async ValueTask OnCancelledAsync(CancelSignal signal, SignalContext signalContext)
    {
        if (!signalContext.IsSelf)
            return;

        var context = signalContext.ReceiverActivityExecutionContext;

        if (!context.GetProperty<bool>(WaitsForChildrenKey))
            return;

        var cancellationToken = signalContext.CancellationToken;
        var store = context.GetRequiredService<IWorkflowInstanceStore>();
        var cancellationService = context.GetRequiredService<IWorkflowCancellationService>();
        var parentInstanceId = context.WorkflowExecutionContext.Id;
        var logger = context.GetRequiredService<ILogger<BulkDispatchWorkflows>>();

        // WorkflowInstanceFilter cannot query a workflow property, so this queries the parent's children and matches the
        // activity instance ID they carry in memory. Querying by that correlation directly needs a new indexed column in
        // every workflow instance store. The query does not filter on status: a child finishing while the pages are read
        // would leave the result set and shift the later pages, so a running child could be skipped. Offset paging is
        // not stable against children being inserted while pages are read; keyset paging (Id > lastId) would close
        // that, but the filter has no such bound.
        var filter = new WorkflowInstanceFilter { ParentWorkflowInstanceIds = [parentInstanceId] };
        var order = new WorkflowInstanceOrder<string>(x => x.Id, OrderDirection.Ascending);
        var pageArgs = PageArgs.FromPage(0, ChildPageSize);
        var pageNumber = 0;
        var consecutiveReadFailures = 0;
        var cancelledCount = 0;

        try
        {
            while (true)
            {
                var (page, failure) = await TryReadChildPageAsync(store, filter, pageArgs, order, cancellationToken, logger, pageNumber);

                if (page is null)
                {
                    consecutiveReadFailures++;

                    if (consecutiveReadFailures >= MaxConsecutiveReadFailures)
                    {
                        logger.LogWarning(
                            failure,
                            "Giving up after {MaxConsecutiveReadFailures} consecutive unreadable pages at page {PageNumber} of parent {ParentWorkflowInstanceId} for activity {ActivityInstanceId}.",
                            MaxConsecutiveReadFailures,
                            pageNumber,
                            parentInstanceId,
                            context.Id);
                        return;
                    }

                    logger.LogWarning(
                        failure,
                        "Failed to read waited-for child workflows of parent {ParentWorkflowInstanceId} for activity {ActivityInstanceId} on page {PageNumber}; skipping this page.",
                        parentInstanceId,
                        context.Id,
                        pageNumber);

                    pageArgs = pageArgs.Next();
                    pageNumber++;
                    continue;
                }

                consecutiveReadFailures = 0;

                try
                {
                    var waitedChildIds = page.Items
                        .Where(x => x.Status == WorkflowStatus.Running && IsWaitedForBy(x.WorkflowState, context.Id))
                        .Select(x => x.Id)
                        .ToList();

                    // The cancellation service hands each child to the workflow cancellation dispatcher, the same way the
                    // workflow-level cascade does, so the children are not cancelled inside this workflow's execution. It also
                    // cancels the children's own children.
                    if (waitedChildIds.Count > 0)
                        cancelledCount += await cancellationService.CancelWorkflowsAsync(waitedChildIds, cancellationToken);
                }
                catch (Exception ex) when (!IsCallerCancellation(ex, cancellationToken))
                {
                    logger.LogWarning(
                        ex,
                        "Failed to cancel waited-for child workflows of parent {ParentWorkflowInstanceId} for activity {ActivityInstanceId} on page {PageNumber}.",
                        parentInstanceId,
                        context.Id,
                        pageNumber);
                }

                if (page.Items.Count < ChildPageSize)
                    return;

                pageArgs = pageArgs.Next();
                pageNumber++;
            }
        }
        finally
        {
            logger.LogDebug(
                "Requested cancellation of {CancelledCount} waited-for child workflows of parent {ParentWorkflowInstanceId} for activity {ActivityInstanceId}.",
                cancelledCount,
                parentInstanceId,
                context.Id);
        }
    }

    /// <summary>
    /// Reads one page of the parent's children, retrying once after a non-caller failure.
    /// </summary>
    private static async ValueTask<(Page<WorkflowInstance>? Page, Exception? Failure)> TryReadChildPageAsync(
        IWorkflowInstanceStore store,
        WorkflowInstanceFilter filter,
        PageArgs pageArgs,
        WorkflowInstanceOrder<string> order,
        CancellationToken cancellationToken,
        ILogger logger,
        int pageNumber)
    {
        try
        {
            return (await store.FindManyAsync(filter, pageArgs, order, cancellationToken), null);
        }
        catch (Exception ex) when (!IsCallerCancellation(ex, cancellationToken))
        {
            logger.LogDebug(
                ex,
                "Retrying waited-for child page {PageNumber} after a failed read.",
                pageNumber);

            try
            {
                return (await store.FindManyAsync(filter, pageArgs, order, cancellationToken), null);
            }
            catch (Exception retryEx) when (!IsCallerCancellation(retryEx, cancellationToken))
            {
                return (null, retryEx);
            }
        }
    }

    private static bool IsCallerCancellation(Exception exception, CancellationToken cancellationToken) =>
        exception is OperationCanceledException && cancellationToken.IsCancellationRequested;

    private static bool IsWaitedForBy(WorkflowState child, string activityInstanceId) =>
        child.Properties.TryGetValue<string>(WorkflowDispatchProperties.WaitingActivityInstanceId, out var waitingActivityInstanceId) && waitingActivityInstanceId == activityInstanceId;
}
