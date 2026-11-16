using System.Text.Json;
using Elsa.Workflows.Management;
using Elsa.Workflows.Runtime.Middleware.Activities;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.State;
using Microsoft.Extensions.DependencyInjection;
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
        var activityNodeId = scheduledBackgroundActivity.ActivityNodeId;

        // These failures happen before the activity runs and leave nothing in the workflow that could record them, so the
        // background job fails and the workflow is not resumed.
        var workflowInstance = await workflowInstanceManager.FindByIdAsync(workflowInstanceId, cancellationToken)
                               ?? throw new InvalidOperationException($"Cannot run background activity {activityNodeId}: workflow instance {workflowInstanceId} was not found.");
        var workflowState = workflowInstance.WorkflowState;
        var workflow = await workflowDefinitionService.FindWorkflowGraphAsync(workflowInstance.DefinitionVersionId, cancellationToken)
                       ?? throw new InvalidOperationException($"Cannot run background activity {activityNodeId} of workflow instance {workflowInstanceId}: workflow definition version {workflowInstance.DefinitionVersionId} was not found.");
        var workflowExecutionContext = await WorkflowExecutionContext.CreateAsync(serviceProvider, workflow, workflowState, cancellationToken: cancellationToken);
        var activityExecutionContext = workflowExecutionContext.ActivityExecutionContexts.FirstOrDefault(x => x.NodeId == activityNodeId)
                                       ?? throw new InvalidOperationException($"Cannot run background activity {activityNodeId} of workflow instance {workflowInstanceId}: the activity is no longer active in the workflow.");

        ExceptionState? faultState = null;

        using (workflowHeartbeatGeneratorFactory.CreateHeartbeatGenerator(workflowExecutionContext))
        {
            await variablePersistenceManager.LoadVariablesAsync(workflowExecutionContext);
            activityExecutionContext.SetIsBackgroundExecution();

            try
            {
                await activityInvoker.InvokeAsync(activityExecutionContext);
            }
            // Only a cancellation of this job skips the resume. Other cancellation exceptions, such as an HttpClient timeout,
            // are activity faults like any other exception.
            catch (Exception e) when (!(e is OperationCanceledException && cancellationToken.IsCancellationRequested))
            {
                _logger.LogWarning(e, "Background execution for activity {ActivityNodeId} faulted", activityNodeId);
                faultState = ExceptionState.FromException(e);
            }

            try
            {
                await variablePersistenceManager.SaveVariablesAsync(workflowExecutionContext);
            }
            // A save failure must not lose the activity fault, or the workflow would stay suspended without an incident.
            catch (Exception saveException) when (faultState != null)
            {
                _logger.LogError(saveException, "Saving variables for faulted background activity {ActivityNodeId} failed", activityNodeId);
            }
        }

        if (faultState != null)
        {
            await ResumeWorkflowWithFaultAsync(activityExecutionContext, scheduledBackgroundActivity, faultState);
        }
        else
        {
            await ResumeWorkflowAsync(activityExecutionContext, scheduledBackgroundActivity);
        }
    }

    private async Task ResumeWorkflowWithFaultAsync(ActivityExecutionContext activityExecutionContext, ScheduledBackgroundActivity scheduledBackgroundActivity, ExceptionState faultState)
    {
        var cancellationToken = activityExecutionContext.CancellationToken;
        var activityNodeId = scheduledBackgroundActivity.ActivityNodeId;
        if (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Background execution for activity {ActivityNodeId} was canceled", activityNodeId);
            return;
        }

        // Send what the activity wrote before it threw, so the workflow sees the same values as after a foreground fault.
        var bookmarkProps = await GetCapturedActivityStateAsync(activityExecutionContext, activityNodeId);
        var faultKey = BackgroundActivityInvokerMiddleware.GetBackgroundActivityFaultKey(activityNodeId);
        bookmarkProps[faultKey] = serviceProvider.GetRequiredService<IPayloadSerializer>().Serialize(faultState);

        await EnqueueResumeAsync(scheduledBackgroundActivity, bookmarkProps, cancellationToken);
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

        var outcomesKey = BackgroundActivityInvokerMiddleware.GetBackgroundActivityOutcomesKey(activityNodeId);
        var completedKey = BackgroundActivityInvokerMiddleware.GetBackgroundActivityCompletedKey(activityNodeId);
        var bookmarksKey = BackgroundActivityInvokerMiddleware.GetBackgroundActivityBookmarksKey(activityNodeId);
        var scheduledActivitiesKey = BackgroundActivityInvokerMiddleware.GetBackgroundActivityScheduledActivitiesKey(activityNodeId);
        var outcomes = activityExecutionContext.GetBackgroundOutcomes()?.ToList();
        var completed = activityExecutionContext.GetBackgroundCompleted();
        var scheduledActivities = activityExecutionContext.GetBackgroundScheduledActivities().ToList();
        var bookmarkProps = await GetCapturedActivityStateAsync(activityExecutionContext, activityNodeId);
        bookmarkProps[scheduledActivitiesKey] = JsonSerializer.Serialize(scheduledActivities);
        bookmarkProps[bookmarksKey] = serviceProvider.GetRequiredService<IPayloadSerializer>().Serialize(activityExecutionContext.Bookmarks.ToList()); // JSON text keeps each bookmark payload's type (#8804).

        if (outcomes != null) bookmarkProps[outcomesKey] = outcomes;
        if (completed != null) bookmarkProps[completedKey] = completed;

        await EnqueueResumeAsync(scheduledBackgroundActivity, bookmarkProps, cancellationToken);
    }

    /// <summary>
    /// Captures the output, journal data and properties that the activity wrote during the background run.
    /// </summary>
    private async Task<Dictionary<string, object>> GetCapturedActivityStateAsync(ActivityExecutionContext activityExecutionContext, string activityNodeId)
    {
        var outputKey = BackgroundActivityInvokerMiddleware.GetBackgroundActivityOutputKey(activityNodeId);
        var journalDataKey = BackgroundActivityInvokerMiddleware.GetBackgroundActivityJournalDataKey(activityNodeId);
        var propsKey = BackgroundActivityInvokerMiddleware.GetBackgroundActivityPropertiesKey(activityNodeId);
        var outputValues = await activityPropertyLogPersistenceEvaluator.GetPersistableOutputAsync(activityExecutionContext);

        return new Dictionary<string, object>
        {
            [outputKey] = outputValues,
            [journalDataKey] = activityExecutionContext.JournalData,
            [propsKey] = activityExecutionContext.Properties.ToDictionary() // ChangeTrackingDictionary is not persistable, so we need to create a copy of the dictionary.
        };
    }

    private async Task EnqueueResumeAsync(ScheduledBackgroundActivity scheduledBackgroundActivity, IDictionary<string, object> bookmarkProps, CancellationToken cancellationToken)
    {
        var resumeBookmarkOptions = new ResumeBookmarkOptions
        {
            Properties = bookmarkProps,
        };
        var enqueuedBookmark = new NewBookmarkQueueItem
        {
            WorkflowInstanceId = scheduledBackgroundActivity.WorkflowInstanceId,
            BookmarkId = scheduledBackgroundActivity.BookmarkId,
            Options = resumeBookmarkOptions
        };
        await bookmarkQueue.EnqueueAsync(enqueuedBookmark, cancellationToken);
    }
}