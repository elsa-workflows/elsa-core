using System.Threading.Channels;

namespace Elsa.Mediator.Contracts;

/// <summary>
/// A channel that can be used to enqueue commands.
/// </summary>
public interface ICommandsChannel
{
    /// <summary>
    /// Gets the writer for the commands queue.
    /// </summary>
    ChannelWriter<ICommand> Writer { get; }
    
    /// <summary>
    /// Gets the reader for the commands queue.
    /// </summary>
    ChannelReader<ICommand> Reader { get; }
}