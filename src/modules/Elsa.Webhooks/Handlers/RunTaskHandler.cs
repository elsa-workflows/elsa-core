using Elsa.Common.Contracts;
using Elsa.Mediator.Contracts;
using Elsa.Webhooks.Models;
using Elsa.Webhooks.Services;
using Elsa.Workflows.Runtime.Notifications;

namespace Elsa.Webhooks.Handlers;

/// <summary>
/// Handles the <see cref="RunTaskRequest"/> notification and asynchronously invokes all registered webhook endpoints.
/// </summary>
public class RunTaskHandler : INotificationHandler<RunTaskRequest>
{
    private readonly IWebhookDispatcher _webhookDispatcher;
    private readonly ISystemClock _systemClock;

    /// <summary>
    /// Constructor.
    /// </summary>
    public RunTaskHandler(IWebhookDispatcher webhookDispatcher, ISystemClock systemClock)
    {
        _webhookDispatcher = webhookDispatcher;
        _systemClock = systemClock;
    }
    
    /// <inheritdoc />
    public async Task HandleAsync(RunTaskRequest notification, CancellationToken cancellationToken)
    {
        var payload = new RunTaskWebhook(notification.TaskId, notification.TaskName, notification.TaskParams);
        var now = _systemClock.UtcNow;
        var webhookEvent = new WebhookEvent("RunTask", payload, now);
        await _webhookDispatcher.DispatchAsync(webhookEvent, cancellationToken);
    }
}