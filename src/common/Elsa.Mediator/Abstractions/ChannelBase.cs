using System.Threading.Channels;

namespace Elsa.Mediator.Abstractions;

/// <summary>
/// A base class for channels.
/// </summary>
/// <typeparam name="T">The type of the items in the channel.</typeparam>
public abstract class ChannelBase<T>
{
    private readonly Channel<T> _channel;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChannelBase{T}"/> class.
    /// </summary>
    protected ChannelBase()
    {
        _channel = Channel.CreateUnbounded<T>();
    }
    
    /// <summary>
    /// Gets the writer for the channel.
    /// </summary>
    public ChannelWriter<T> Writer => _channel.Writer;
    
    /// <summary>
    /// Gets the reader for the channel.
    /// </summary>
    public ChannelReader<T> Reader => _channel.Reader;
}