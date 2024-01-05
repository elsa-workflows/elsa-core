using Elsa.Common.Contracts;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Pipelines.WorkflowExecution;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Notifications;

namespace Elsa.Workflows.Runtime.Middleware.Workflows;

/// <summary>
/// Creates and updates activity execution records from activity execution contexts.
/// </summary>
public class PersistActivityExecutionLogMiddleware : WorkflowExecutionMiddleware
{
    private readonly IActivityExecutionStore _activityExecutionStore;
    private readonly IActivityExecutionMapper _activityExecutionMapper;
    private readonly INotificationSender _notificationSender;
    private readonly ITenantAccessor _tenantAccessor;

    /// <inheritdoc />
    public PersistActivityExecutionLogMiddleware(
        WorkflowMiddlewareDelegate next,
        IActivityExecutionStore activityExecutionStore,
        IActivityExecutionMapper activityExecutionMapper,
        INotificationSender notificationSender,
        ITenantAccessor tenantAccessor) : base(next)
    {
        _activityExecutionStore = activityExecutionStore;
        _activityExecutionMapper = activityExecutionMapper;
        _notificationSender = notificationSender;
        _tenantAccessor = tenantAccessor;
    }

    /// <inheritdoc />
    public override async ValueTask InvokeAsync(WorkflowExecutionContext context)
    {
        _tenantAccessor.SetCurrentTenantId(context.Workflow.WorkflowMetadata.TenantId);

        // Invoke next middleware.
        await Next(context);

        // Get the managed cancellation token.
        var cancellationToken = context.CancellationTokens.SystemCancellationToken;

        // Get all activity execution contexts.
        var activityExecutionContexts = context.ActivityExecutionContexts;

        // Persist activity execution entries.
        var entries = activityExecutionContexts.Select(_activityExecutionMapper.Map).ToList();

        await _activityExecutionStore.SaveManyAsync(entries, cancellationToken);
        await _notificationSender.SendAsync(new ActivityExecutionLogUpdated(context, entries), cancellationToken);
    }
}