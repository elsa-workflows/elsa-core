using Elsa.Extensions;
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
        var entries = activityExecutionContexts.Select(activityExecutionContext =>
        {
            // Get any outcomes that were added to the activity execution context.
            var outcomes = activityExecutionContext.JournalData.TryGetValue("Result", out var resultValue) ? resultValue as Outcomes : default;
            var payload = new Dictionary<string, object>();
            
            if(outcomes != null)
                payload.Add("Outcomes", outcomes);
            
            // Get any outputs that were added to the activity execution context.
            var activity = activityExecutionContext.Activity;
            var expressionExecutionContext = activityExecutionContext.ExpressionExecutionContext;
            var activityDescriptor = activityExecutionContext.ActivityDescriptor;
            var outputDescriptors = activityDescriptor.Outputs;
            var outputs = outputDescriptors.ToDictionary(x => x.Name, x => activity.GetOutput(expressionExecutionContext, x.Name)!);
            
            return new ActivityExecutionRecord
            {
                Id = activityExecutionContext.Id,
                ActivityId = activityExecutionContext.Activity.Id,
                WorkflowInstanceId = context.Id,
                ActivityType = activityExecutionContext.Activity.Type,
                ActivityName = activityExecutionContext.Activity.Name,
                ActivityState = activityExecutionContext.ActivityState,
                Outputs = outputs,
                Payload = payload,
                Exception = ExceptionState.FromException(activityExecutionContext.Exception),
                ActivityTypeVersion = activityExecutionContext.Activity.Version,
                StartedAt = activityExecutionContext.StartedAt,
                HasBookmarks = activityExecutionContext.Bookmarks.Any(),
                Status = activityExecutionContext.Status,
                CompletedAt = activityExecutionContext.CompletedAt
            };
        }).ToList();

        await _activityExecutionStore.SaveManyAsync(entries, context.CancellationToken);
        await _notificationSender.SendAsync(new ActivityExecutionLogUpdated(context, entries));
    }
}