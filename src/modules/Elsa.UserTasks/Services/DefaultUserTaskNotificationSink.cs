using Elsa.Mediator.Contracts;
using Elsa.UserTasks.Contracts;
using Elsa.UserTasks.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.UserTasks.Services;

public sealed class DefaultUserTaskNotificationSink(IServiceProvider services) : IUserTaskNotificationSink
{
    public Task PublishAsync(UserTaskLifecycleNotification notification, CancellationToken cancellationToken = default)
    {
        return services.GetService<INotificationSender>()?.SendAsync(notification, cancellationToken) ?? Task.CompletedTask;
    }
}
