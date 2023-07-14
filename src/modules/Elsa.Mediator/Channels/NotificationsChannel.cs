using Elsa.Mediator.Abstractions;
using Elsa.Mediator.Contracts;

namespace Elsa.Mediator.Channels;

/// <inheritdoc cref="Elsa.Mediator.Contracts.INotificationsChannel" />
public class NotificationsChannel : ChannelBase<INotification>, INotificationsChannel
{
}