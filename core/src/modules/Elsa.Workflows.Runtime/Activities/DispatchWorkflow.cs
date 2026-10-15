using System.Runtime.CompilerServices;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Stimuli;
using Elsa.Workflows.Runtime.UIHints;
using Elsa.Workflows.Signals;
using Elsa.Workflows.UIHints;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime.Activities;

/// <summary>
/// Creates a new workflow instance of the specified workflow and dispatches it for execution.
/// When <see cref="WaitForCompletion"/> is enabled and activation is denied, the activity completes with a result dictionary containing
/// <c>CannotStart</c> set to <c>true</c> and the requested <c>WorkflowInstanceId</c>.
/// When <see cref="WaitForCompletion"/> is enabled and the activity is cancelled while it waits, the child workflow it is waiting for is cancelled too.
/// With the transactional outbox on, when the activity is cancelled in the burst that dispatched the child and the parent has not
/// committed since the dispatch, the child's pending dispatch is withdrawn from the outbox and the child is never created. Otherwise this only reaches a child that has made its
/// first commit: until then a new child exists only in memory and is invisible to the workflow instance store. A child is missed when the
/// activity is cancelled in the same burst that dispatched it without the outbox, while the dispatch queue or message transport is
/// backlogged, or while the child's first burst is still running. Cancelling the parent workflow, which cascades by parent instance ID,
/// has the same gap.
/// </summary>
[Activity("Elsa", "Composition", "Create a new workflow instance of the specified workflow and dispatch it for execution.")]
[UsedImplicitly]
public class DispatchWorkflow : Activity<object>
{
    private const string WaitedChildInstanceIdKey = "WaitedChildInstanceId";

    /// <inheritdoc />
    public DispatchWorkflow([CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : base(source, line)
    {
        OnSignalReceived<CancelSignal>(OnCancelledAsync);
    }

    /// <summary>
    /// The definition ID of the workflow to dispatch. 
    /// </summary>
    [Input(
        DisplayName = "Workflow Definition",
        Description = "The definition ID of the workflow to dispatch.",
        UIHint = InputUIHints.WorkflowDefinitionPicker
    )]
    public Input<string> WorkflowDefinitionId { get; set; } = null!;

    /// <summary>
    /// The correlation ID to associate the workflow with. 
    /// </summary>
    [Input(
        DisplayName = "Correlation ID",
        Description = "The correlation ID to associate the workflow with."
    )]
    public Input<string?> CorrelationId { get; set; } = null!;

    /// <summary>
    /// The input to send to the workflow.
    /// </summary>
    [Input(Description = "The input to send to the workflow.")]
    public Input<IDictionary<string, object>?> Input { get; set; } = null!;

    /// <summary>
    /// True to wait for the child workflow to complete before completing this activity, false to "fire and forget".
    /// </summary>
    [Input(Description = "Wait for the child workflow to complete before completing this activity.")]
    public Input<bool> WaitForCompletion { get; set; } = null!;

    /// <summary>
    /// Indicates whether a new trace context should be started for the workflow execution.
    /// </summary>
    [Input(Description = "Start a new trace context when using Open Telemetry.", Category = "Open Telemetry")]
    public Input<bool> StartNewTrace { get; set; } = null!;

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

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var waitForCompletion = WaitForCompletion.GetOrDefault(context);

        // Dispatch the child workflow.
        var instanceId = await DispatchChildWorkflowAsync(context, waitForCompletion);

        // If we need to wait for the child workflow to complete, create a bookmark.
        if (waitForCompletion)
        {
            // Remember the child this activity waits for, so cancelling the activity can cancel exactly that instance.
            context.SetProperty(WaitedChildInstanceIdKey, instanceId);

            var bookmarkOptions = new CreateBookmarkArgs
            {
                Callback = OnChildWorkflowCompletedAsync,
                Stimulus = new DispatchWorkflowStimulus(instanceId),
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

    private async ValueTask<string> DispatchChildWorkflowAsync(ActivityExecutionContext context, bool waitForCompletion)
    {
        var workflowDefinitionId = WorkflowDefinitionId.Get(context);
        var workflowDefinitionService = context.GetRequiredService<IWorkflowDefinitionService>();
        var workflowGraph = await workflowDefinitionService.FindWorkflowGraphAsync(workflowDefinitionId, VersionOptions.Published, context.CancellationToken);

        if (workflowGraph == null)
            throw new($"No published version of workflow definition with ID {workflowDefinitionId} found.");

        var input = Input.GetOrDefault(context) ?? new Dictionary<string, object>();
        var channelName = ChannelName.GetOrDefault(context);
        var startNewTrace = StartNewTrace.GetOrDefault(context);
        var parentInstanceId = context.WorkflowExecutionContext.Id;
        var properties = new Dictionary<string, object>
        {
            ["ParentInstanceId"] = parentInstanceId,
        };

        // If we need to wait for the child workflow to complete, set the property. This will be used by the ResumeDispatchWorkflowActivity handler.
        if (waitForCompletion) properties[WorkflowDispatchProperties.WaitForCompletion] = true;
        if (startNewTrace) properties["StartNewTrace"] = true;
        
        input["ParentInstanceId"] = parentInstanceId;

        var correlationId = CorrelationId.GetOrDefault(context);
        var workflowDispatcher = context.GetRequiredService<IWorkflowDispatcher>();
        var identityGenerator = context.GetRequiredService<IIdentityGenerator>();
        var instanceId = identityGenerator.GenerateId();

        if (waitForCompletion)
        {
            var activityTypeName = ActivityTypeNameHelper.GenerateTypeName<DispatchWorkflow>();
            var stimulus = new DispatchWorkflowStimulus(instanceId);
            var stimulusHash = context.GetRequiredService<IStimulusHasher>().Hash(activityTypeName, stimulus);
            DispatchWorkflowActivationDeniedRoute.Add(properties, activityTypeName, stimulusHash);
        }

        var request = new DispatchWorkflowDefinitionRequest(workflowGraph.Workflow.Identity.Id)
        {
            ParentWorkflowInstanceId = parentInstanceId,
            Input = input,
            Properties = properties,
            CorrelationId = correlationId,
            InstanceId = instanceId,
            SchedulingActivityExecutionId = context.Id,
            SchedulingWorkflowInstanceId = parentInstanceId,
            SchedulingCallStackDepth = context.CallStackDepth
        };
        var options = new DispatchWorkflowOptions
        {
            Channel = channelName
        };

        // Dispatch the child workflow.
        var dispatchResponse = await workflowDispatcher.DispatchAsync(request, options, context.CancellationToken);
        dispatchResponse.ThrowIfFailed();

        return instanceId;
    }

    private async ValueTask OnChildWorkflowCompletedAsync(ActivityExecutionContext context)
    {
        var input = context.WorkflowInput;
        context.Set(Result, input);
        await context.CompleteActivityAsync();
    }

    /// <summary>
    /// Cancels the child this activity was waiting for. When this burst dispatched it through the transactional outbox,
    /// the pending dispatch is withdrawn first; if the parent has not committed since the dispatch, the child is never created.
    /// After a mid-burst commit, a delivery already in flight can still create the child, and if that delivery fails the
    /// failure path may save the withdrawn row again until this cancel burst commits. Only that instance
    /// is targeted: cancelling by parent instance would also reach fire-and-forget and sibling dispatches of the same
    /// parent. A child that has already finished is left alone, together with anything it dispatched. If the child finishes between the read and the cancellation,
    /// the cancellation service still cascades to that child's own children. A failure looking up or cancelling the child
    /// is logged so this best-effort cleanup does not fault the parent's cancel path.
    /// </summary>
    private async ValueTask OnCancelledAsync(CancelSignal signal, SignalContext signalContext)
    {
        if (!signalContext.IsSelf)
            return;

        var context = signalContext.ReceiverActivityExecutionContext;
        var childInstanceId = context.GetProperty<string>(WaitedChildInstanceIdKey);

        if (string.IsNullOrEmpty(childInstanceId))
            return;

        var cancellationToken = signalContext.CancellationToken;
        await context.WorkflowExecutionContext.WithdrawPendingChildDispatchAsync(childInstanceId, cancellationToken);

        try
        {
            var child = await context.GetRequiredService<IWorkflowInstanceStore>().FindAsync(new WorkflowInstanceFilter { Id = childInstanceId }, cancellationToken);

            if (child is not { Status: WorkflowStatus.Running })
                return;

            // The cancellation service hands the child to the workflow cancellation dispatcher, the same way the
            // workflow-level cascade does, so the child is not cancelled inside this workflow's execution. It also cancels
            // the child's own children.
            await context.GetRequiredService<IWorkflowCancellationService>().CancelWorkflowAsync(childInstanceId, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            context.GetRequiredService<ILogger<DispatchWorkflow>>().LogWarning(
                ex,
                "Failed to cancel waited-for child workflow {ChildWorkflowInstanceId} of parent {ParentWorkflowInstanceId} for activity {ActivityInstanceId}.",
                childInstanceId,
                context.WorkflowExecutionContext.Id,
                context.Id);
        }
    }
}
