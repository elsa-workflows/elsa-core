using System.Runtime.CompilerServices;
using Elsa.Common.Models;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Workflows.Activities.Flowchart.Attributes;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Management;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Stimuli;
using Elsa.Workflows.Runtime.UIHints;
using Elsa.Workflows.UIHints;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Activities;

/// <summary>
/// Creates new workflow instances of the specified workflow for each item in the data source and dispatches them for execution.
/// </summary>
[Activity("Elsa", "Composition", "Create new workflow instances for each item in the data source and dispatch them for execution.", Kind = ActivityKind.Task)]
[FlowNode("Completed", "Canceled", "Done")]
[UsedImplicitly]
public class BulkDispatchWorkflows : Activity
{
    private const string DispatchedInstancesCountKey = nameof(DispatchedInstancesCountKey);
    private const string CompletedInstancesCountKey = nameof(CompletedInstancesCountKey);

    /// <inheritdoc />
    public BulkDispatchWorkflows([CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : base(source, line)
    {
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
            await DispatchChildWorkflowAsync(context, item, waitForCompletion, startNewTrace);

        // Store the number of dispatched instances for tracking.
        context.SetProperty(DispatchedInstancesCountKey, count);

        // If we need to wait for the child workflows to complete (if any), create a bookmark.
        if (waitForCompletion && count > 0)
        {
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

    private async ValueTask<string> DispatchChildWorkflowAsync(ActivityExecutionContext context, object item, bool waitForCompletion, bool startNewTrace)
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

        if (waitForCompletion) properties["WaitForCompletion"] = true;
        if (startNewTrace) properties["StartNewTrace"] = true;

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
        var finishedInstancesCount = context.GetProperty<long>(CompletedInstancesCountKey) + 1;

        context.SetProperty(CompletedInstancesCountKey, finishedInstancesCount);

        var childInstanceId = new Variable<string>("ChildInstanceId", workflowInstanceId)
        {
            StorageDriverType = typeof(WorkflowInstanceStorageDriver)
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
}