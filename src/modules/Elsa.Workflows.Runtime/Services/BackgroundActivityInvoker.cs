using System.Text.Json;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Middleware.Activities;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Services;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// An activity invoker that invokes activities detached from the workflow. This is useful for invoking activities from a background worker.
/// </summary>
public class BackgroundActivityInvoker(
    IBookmarkResumer bookmarkResumer,
    IWorkflowInstanceManager workflowInstanceManager,
    IWorkflowDefinitionService workflowDefinitionService,
    IVariablePersistenceManager variablePersistenceManager,
    IActivityInvoker activityInvoker,
    IServiceProvider serviceProvider,
    ILogger<BackgroundActivityInvoker> logger)
    : IBackgroundActivityInvoker
{
    private readonly ILogger _logger = logger;

    /// <inheritdoc />
    public async Task ExecuteAsync(ScheduledBackgroundActivity scheduledBackgroundActivity, CancellationToken cancellationToken = default)
    {
        var workflowInstanceId = scheduledBackgroundActivity.WorkflowInstanceId;
        var workflowInstance = await workflowInstanceManager.FindByIdAsync(workflowInstanceId, cancellationToken);
        if (workflowInstance == null) throw new Exception("Workflow instance not found");
        var workflowState = workflowInstance.WorkflowState;
        var workflow = await workflowDefinitionService.FindWorkflowAsync(workflowInstance.DefinitionVersionId, cancellationToken);
        if (workflow == null) throw new Exception("Workflow definition not found");
        var workflowExecutionContext = await WorkflowExecutionContext.CreateAsync(serviceProvider, workflow, workflowState, cancellationToken: cancellationToken);
        var activityNodeId = scheduledBackgroundActivity.ActivityNodeId;
        var activityExecutionContext = workflowExecutionContext.ActivityExecutionContexts.First(x => x.NodeId == activityNodeId);

        await variablePersistenceManager.LoadVariablesAsync(workflowExecutionContext);
        activityExecutionContext.SetIsBackgroundExecution();
        await activityInvoker.InvokeAsync(activityExecutionContext);
        await ResumeWorkflowAsync(activityExecutionContext, scheduledBackgroundActivity);
    }

    private async Task ResumeWorkflowAsync(ActivityExecutionContext activityExecutionContext, ScheduledBackgroundActivity scheduledBackgroundActivity)
    {
        var cancellationToken = activityExecutionContext.CancellationToken;
        var activityNodeId = scheduledBackgroundActivity.ActivityNodeId;
        if (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Background execution for activity {ActivityNodeId} was canceled", activityNodeId);
            return;
        }

        var inputKey = BackgroundActivityInvokerMiddleware.GetBackgroundActivityOutputKey(activityNodeId);
        var outcomesKey = BackgroundActivityInvokerMiddleware.GetBackgroundActivityOutcomesKey(activityNodeId);
        var completedKey = BackgroundActivityInvokerMiddleware.GetBackgroundActivityCompletedKey(activityNodeId);
        var journalDataKey = BackgroundActivityInvokerMiddleware.GetBackgroundActivityJournalDataKey(activityNodeId);
        var bookmarksKey = BackgroundActivityInvokerMiddleware.GetBackgroundActivityBookmarksKey(activityNodeId);
        var scheduledActivitiesKey = BackgroundActivityInvokerMiddleware.GetBackgroundActivityScheduledActivitiesKey(activityNodeId);
        var outcomes = activityExecutionContext.GetBackgroundOutcomes()?.ToList();
        var completed = activityExecutionContext.GetBackgroundCompleted();
        var scheduledActivities = activityExecutionContext.GetBackgroundScheduledActivities().ToList();
        var workflowInstanceId = scheduledBackgroundActivity.WorkflowInstanceId;
        var outputValues = ExtractActivityOutput(activityExecutionContext);
        var properties = new Dictionary<string, object>
        {
            [scheduledActivitiesKey] = JsonSerializer.Serialize(scheduledActivities),
            [inputKey] = outputValues,
            [journalDataKey] = activityExecutionContext.JournalData,
            [bookmarksKey] = activityExecutionContext.Bookmarks.ToList()
        };

        if (outcomes != null) properties[outcomesKey] = outcomes;
        if (completed != null) properties[completedKey] = completed;

        var bookmarkFilter = new BookmarkFilter
        {
            BookmarkId = scheduledBackgroundActivity.BookmarkId,
            WorkflowInstanceId = workflowInstanceId
        };
        var resumeBookmarkOptions = new ResumeBookmarkOptions
        {
            Properties = properties
        };
        await bookmarkResumer.ResumeAsync(bookmarkFilter, resumeBookmarkOptions, cancellationToken);
    }

    private IDictionary<string, object> ExtractActivityOutput(ActivityExecutionContext activityExecutionContext)
    {
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

        return outputValues;
    }
}