using System.Threading.Channels;
using Elsa.Mediator.Contracts;
using Elsa.Mediator.Models;

namespace Elsa.Mediator.Services;

/// <inheritdoc />
public class NotificationsChannel : INotificationsChannel
{
    private readonly Channel<INotification> _channel;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobsChannel"/> class.
    /// </summary>
    public NotificationsChannel()
    {
        _channel = Channel.CreateUnbounded<INotification>();
    }

    /// <inheritdoc />
    public ChannelWriter<INotification> Writer => _channel.Writer;

    /// <inheritdoc />
    public ChannelReader<INotification> Reader => _channel.Reader;
}