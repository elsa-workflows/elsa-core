using System.Threading.Channels;

namespace Elsa.Workflows.Runtime;

public class BookmarkQueueSignaler : IBookmarkQueueSignaler
{
    private readonly Channel<object?> _channel;

    public BookmarkQueueSignaler()
    {
        var options = new BoundedChannelOptions(1)
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        };
        _channel = Channel.CreateBounded<object?>(options);
    }

    public Task AwaitAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAsync(cancellationToken).AsTask();
    }

    public Task TriggerAsync(CancellationToken cancellationToken)
    {
        _channel.Writer.TryWrite(null);
        return Task.CompletedTask;
    }
}