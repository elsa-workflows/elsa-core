using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Elsa.Common.Models;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Middleware.Activities;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Services;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// An activity invoker that invokes activities detached from the workflow. This is useful for invoking activities from a background worker.
/// </summary>
public class DefaultBackgroundActivityInvoker : IBackgroundActivityInvoker
{
    private readonly IWorkflowRuntime _workflowRuntime;
    private readonly IWorkflowDispatcher _workflowDispatcher;
    private readonly IWorkflowDefinitionService _workflowDefinitionService;
    private readonly IVariablePersistenceManager _variablePersistenceManager;
    private readonly IActivityInvoker _activityInvoker;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultBackgroundActivityInvoker"/> class.
    /// </summary>
    public DefaultBackgroundActivityInvoker(
        IWorkflowRuntime workflowRuntime,
        IWorkflowDispatcher workflowDispatcher,
        IWorkflowDefinitionService workflowDefinitionService,
        IVariablePersistenceManager variablePersistenceManager,
        IActivityInvoker activityInvoker,
        IServiceProvider serviceProvider,
        ILogger<DefaultBackgroundActivityInvoker> logger)
    {
        _workflowRuntime = workflowRuntime;
        _workflowDispatcher = workflowDispatcher;
        _workflowDefinitionService = workflowDefinitionService;
        _variablePersistenceManager = variablePersistenceManager;
        _activityInvoker = activityInvoker;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("The activity type may be trimmed")]
    public async Task ExecuteAsync(ScheduledBackgroundActivity scheduledBackgroundActivity, CancellationToken cancellationToken = default)
    {
        var workflowInstanceId = scheduledBackgroundActivity.WorkflowInstanceId;
        var workflowState = await _workflowRuntime.ExportWorkflowStateAsync(workflowInstanceId, cancellationToken);

        if (workflowState == null)
            throw new Exception("Workflow state not found");

        var workflow = await _workflowDefinitionService.FindWorkflowAsync(workflowState.DefinitionId, VersionOptions.SpecificVersion(workflowState.DefinitionVersion), cancellationToken);

        if (workflow == null)
            throw new Exception("Workflow definition not found");
        
        var workflowExecutionContext = await WorkflowExecutionContext.CreateAsync(_serviceProvider, workflow, workflowState, cancellationTokens: cancellationToken);
        var activityNodeId = scheduledBackgroundActivity.ActivityNodeId;
        var activityExecutionContext = workflowExecutionContext.ActivityExecutionContexts.First(x => x.NodeId == activityNodeId);

        // Load persistent variables for the activity to use.
        await _variablePersistenceManager.LoadVariablesAsync(workflowExecutionContext);

        // Mark the activity as being invoked from a background worker.
        activityExecutionContext.SetIsBackgroundExecution();

        // Invoke the activity.
        await _activityInvoker.InvokeAsync(activityExecutionContext);

        // Capture any activity output produced by the activity (but only if the associated memory block is stored in the workflow itself).
        var outputDescriptors = activityExecutionContext.ActivityDescriptor.Outputs;
        var outputValues = new Dictionary<string, object>();

        foreach (var outputDescriptor in outputDescriptors)
        {
            var output = (Output?)outputDescriptor.ValueGetter(activityExecutionContext.Activity);

            if (output == null)
                continue;

            var memoryBlockReference = output.MemoryBlockReference();

            if (!activityExecutionContext.ExpressionExecutionContext.TryGetBlock(memoryBlockReference, out var memoryBlock))
                continue;

            var variableMetadata = memoryBlock.Metadata as VariableBlockMetadata;
            var driver = variableMetadata?.StorageDriverType;

            // We only capture output written to the workflow itself. Other drivers like blob storage, etc. will be ignored since the foreground context will be loading those.
            if (driver != typeof(WorkflowStorageDriver))
                continue;

            var outputValue = activityExecutionContext.Get(memoryBlockReference);

            if (outputValue != null)
                outputValues[outputDescriptor.Name] = outputValue;
        }

        // Resume the workflow, passing along activity output, outcomes and scheduled activities.
        var bookmarkId = scheduledBackgroundActivity.BookmarkId;
        var inputKey = BackgroundActivityInvokerMiddleware.GetBackgroundActivityOutputKey(activityNodeId);
        var outcomesKey = BackgroundActivityInvokerMiddleware.GetBackgroundActivityOutcomesKey(activityNodeId);
        var completedKey = BackgroundActivityInvokerMiddleware.GetBackgroundActivityCompletedKey(activityNodeId);
        var journalDataKey = BackgroundActivityInvokerMiddleware.GetBackgroundActivityJournalDataKey(activityNodeId);
        var bookmarksKey = BackgroundActivityInvokerMiddleware.GetBackgroundActivityBookmarksKey(activityNodeId);
        var scheduledActivitiesKey = BackgroundActivityInvokerMiddleware.GetBackgroundActivityScheduledActivitiesKey(activityNodeId);
        var outcomes = activityExecutionContext.GetBackgroundOutcomes()?.ToList();
        var completed = activityExecutionContext.GetBackgroundCompleted();
        var scheduledActivities = activityExecutionContext.GetBackgroundScheduledActivities().ToList();

        var dispatchRequest = new DispatchWorkflowInstanceRequest
        {
            InstanceId = workflowInstanceId,
            BookmarkId = bookmarkId,
            Properties = new Dictionary<string, object>
            {
                [scheduledActivitiesKey] = JsonSerializer.Serialize(scheduledActivities),
                [inputKey] = outputValues,
                [journalDataKey] = activityExecutionContext.JournalData,
                [bookmarksKey] = activityExecutionContext.Bookmarks.ToList()
            }
        };

        if (outcomes != null)
            dispatchRequest.Properties[outcomesKey] = outcomes;

        if (completed != null)
            dispatchRequest.Properties[completedKey] = completed;

        if (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Background execution for activity {ActivityNodeId} was canceled", activityNodeId);
            return;
        }

        await _workflowDispatcher.DispatchAsync(dispatchRequest, cancellationToken: cancellationToken);
    }
}