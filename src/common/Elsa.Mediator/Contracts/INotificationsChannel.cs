using System.Threading.Channels;

namespace Elsa.Mediator.Contracts;

/// <summary>
/// A channel that can be used to enqueue notifications.
/// </summary>
public interface INotificationsChannel
{
    /// <summary>
    /// Gets the writer for the notifications queue.
    /// </summary>
    ChannelWriter<INotification> Writer { get; }
    
    /// <summary>
    /// Gets the reader for the notifications queue.
    /// </summary>
    ChannelReader<INotification> Reader { get; }
}