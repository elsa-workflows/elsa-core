using System.Text.Json;
using Elsa.Workflows.Management;
using Elsa.Workflows.Runtime.Middleware.Activities;
using Elsa.Workflows.Runtime.Options;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// An activity invoker that invokes activities detached from the workflow. This is useful for invoking activities from a background worker.
/// </summary>
public class BackgroundActivityInvoker(
    IBookmarkQueue bookmarkQueue,
    IWorkflowInstanceManager workflowInstanceManager,
    IWorkflowDefinitionService workflowDefinitionService,
    IVariablePersistenceManager variablePersistenceManager,
    IActivityInvoker activityInvoker,
    IActivityPropertyLogPersistenceEvaluator activityPropertyLogPersistenceEvaluator,
    WorkflowHeartbeatGeneratorFactory workflowHeartbeatGeneratorFactory,
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
        if (workflowInstance == null) throw new("Workflow instance not found");
        var workflowState = workflowInstance.WorkflowState;
        var workflow = await workflowDefinitionService.FindWorkflowGraphAsync(workflowInstance.DefinitionVersionId, cancellationToken);
        if (workflow == null) throw new("Workflow definition not found");
        var workflowExecutionContext = await WorkflowExecutionContext.CreateAsync(serviceProvider, workflow, workflowState, cancellationToken: cancellationToken);
        var activityNodeId = scheduledBackgroundActivity.ActivityNodeId;
        var activityExecutionContext = workflowExecutionContext.ActivityExecutionContexts.First(x => x.NodeId == activityNodeId);

        using (workflowHeartbeatGeneratorFactory.CreateHeartbeatGenerator(workflowExecutionContext))
        {
            await variablePersistenceManager.LoadVariablesAsync(workflowExecutionContext);
            activityExecutionContext.SetIsBackgroundExecution();
            await activityInvoker.InvokeAsync(activityExecutionContext);
            await variablePersistenceManager.SaveVariablesAsync(workflowExecutionContext);
        }
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
        var propsKey = BackgroundActivityInvokerMiddleware.GetBackgroundActivityPropertiesKey(activityNodeId);
        var outcomes = activityExecutionContext.GetBackgroundOutcomes()?.ToList();
        var completed = activityExecutionContext.GetBackgroundCompleted();
        var scheduledActivities = activityExecutionContext.GetBackgroundScheduledActivities().ToList();
        var workflowInstanceId = scheduledBackgroundActivity.WorkflowInstanceId;
        var outputValues = await activityPropertyLogPersistenceEvaluator.GetPersistableOutputAsync(activityExecutionContext);
        var bookmarkProps = new Dictionary<string, object>
        {
            [scheduledActivitiesKey] = JsonSerializer.Serialize(scheduledActivities),
            [inputKey] = outputValues,
            [journalDataKey] = activityExecutionContext.JournalData,
            [bookmarksKey] = activityExecutionContext.Bookmarks.ToList(),
            [propsKey] = activityExecutionContext.Properties.ToDictionary() // ChangeTrackingDictionary is not persistable, so we need to create a copy of the dictionary.
        };

        if (outcomes != null) bookmarkProps[outcomesKey] = outcomes;
        if (completed != null) bookmarkProps[completedKey] = completed;

        var resumeBookmarkOptions = new ResumeBookmarkOptions
        {
            Properties = bookmarkProps,
        };
        var enqueuedBookmark = new NewBookmarkQueueItem
        {
            WorkflowInstanceId = workflowInstanceId,
            BookmarkId = scheduledBackgroundActivity.BookmarkId,
            Options = resumeBookmarkOptions
        };
        await bookmarkQueue.EnqueueAsync(enqueuedBookmark, cancellationToken);
    }
}