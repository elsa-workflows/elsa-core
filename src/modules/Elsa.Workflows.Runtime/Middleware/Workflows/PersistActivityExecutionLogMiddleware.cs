using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Pipelines.WorkflowExecution;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Runtime.Middleware.Workflows;

/// <summary>
/// Creates and updates activity execution records from activity execution contexts.
/// </summary>
public class PersistActivityExecutionLogMiddleware : WorkflowExecutionMiddleware
{
    private readonly IActivityExecutionLogStore _activityExecutionLogStore;

    /// <inheritdoc />
    public PersistActivityExecutionLogMiddleware(WorkflowMiddlewareDelegate next, IActivityExecutionLogStore activityExecutionLogStore) : base(next)
    {
        _activityExecutionLogStore = activityExecutionLogStore;
    }

    /// <inheritdoc />
    public override async ValueTask InvokeAsync(WorkflowExecutionContext context)
    {
        // Invoke next middleware.
        await Next(context);

        // Get all activity execution contexts.
        var activityExecutionContexts = context.ActivityExecutionContexts;

        // Persist activity execution entries.
        var entries = activityExecutionContexts.Select(x => new ActivityExecutionRecord
        {
            Id = x.Id,
            ActivityId = x.Activity.Id,
            WorkflowInstanceId = context.Id,
            ActivityType = x.Activity.Type,
            ActivityName = x.Activity.Name,
            ActivityState = x.ActivityState,
            ActivityTypeVersion = x.Activity.Version,
            StartedAt = x.StartedAt,
            HasBookmarks = x.Bookmarks.Any(),
            CompletedAt = x.CompletedAt
        }).ToList();

        await _activityExecutionLogStore.SaveManyAsync(entries, context.CancellationToken);
    }
}