using Elsa.ExternalAuthentication.Notifications;
using Elsa.Mediator.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ExternalAuthentication.Services;

/// <summary>Publishes redacted security events after the caller has committed its state change.</summary>
public sealed class ExternalAuthenticationSecurityNotifier(IServiceProvider services)
{
    public ValueTask PublishAsync(INotification notification, CancellationToken cancellationToken = default)
    {
        var sender = services.GetService<INotificationSender>();
        return sender is null ? ValueTask.CompletedTask : new ValueTask(sender.SendAsync(notification, cancellationToken));
    }

    public static SecurityEventContext Context(
        string? actorId,
        string? tenantId,
        string? connectionId,
        string? userId,
        SecurityEventOutcome outcome,
        string summary) => new(
        actorId,
        tenantId,
        connectionId,
        userId,
        DateTimeOffset.UtcNow,
        outcome,
        Guid.NewGuid().ToString("N"),
        summary);
}
