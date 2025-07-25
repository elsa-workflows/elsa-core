using Elsa.Mediator.Abstractions;
using Elsa.Mediator.Contracts;
using Elsa.Mediator.Middleware.Notification;

namespace Elsa.Mediator.Channels;

/// <inheritdoc cref="Elsa.Mediator.Contracts.INotificationsChannel" />
public class NotificationsChannel : ChannelBase<NotificationContext>, INotificationsChannel
{
}