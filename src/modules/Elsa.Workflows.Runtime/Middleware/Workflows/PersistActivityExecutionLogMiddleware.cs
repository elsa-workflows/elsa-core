using Elsa.Mediator.Contracts;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities.Flowchart.Models;
using Elsa.Workflows.Core.Pipelines.WorkflowExecution;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Notifications;

namespace Elsa.Workflows.Runtime.Middleware.Workflows;

/// <summary>
/// Creates and updates activity execution records from activity execution contexts.
/// </summary>
public class PersistActivityExecutionLogMiddleware : WorkflowExecutionMiddleware
{
    private readonly IActivityExecutionStore _activityExecutionStore;
    private readonly INotificationSender _notificationSender;

    /// <inheritdoc />
    public PersistActivityExecutionLogMiddleware(WorkflowMiddlewareDelegate next, IActivityExecutionStore activityExecutionStore, INotificationSender notificationSender) : base(next)
    {
        _activityExecutionStore = activityExecutionStore;
        _notificationSender = notificationSender;
    }

    /// <inheritdoc />
    public override async ValueTask InvokeAsync(WorkflowExecutionContext context)
    {
        // Invoke next middleware.
        await Next(context);

        // Get all activity execution contexts.
        var activityExecutionContexts = context.ActivityExecutionContexts;
        
        // Persist activity execution entries.
        var entries = activityExecutionContexts.Select(x =>
        {
            // Get any outcomes that were added to the activity execution context.
            var outcomes = x.JournalData.TryGetValue("Result", out var resultValue) ? resultValue as Outcomes : default;
            var payload = new Dictionary<string, object>();
            
            if(outcomes != null)
                payload.Add("Outcomes", outcomes);
            
            return new ActivityExecutionRecord
            {
                Id = x.Id,
                ActivityId = x.Activity.Id,
                WorkflowInstanceId = context.Id,
                ActivityType = x.Activity.Type,
                ActivityName = x.Activity.Name,
                ActivityState = x.ActivityState,
                Payload = payload,
                Exception = ExceptionState.FromException(x.Exception),
                ActivityTypeVersion = x.Activity.Version,
                StartedAt = x.StartedAt,
                HasBookmarks = x.Bookmarks.Any(),
                Status = x.Status,
                CompletedAt = x.CompletedAt
            };
        }).ToList();

        await _activityExecutionStore.SaveManyAsync(entries, context.CancellationToken);
        await _notificationSender.SendAsync(new ActivityExecutionLogUpdated(context, entries));
    }
}