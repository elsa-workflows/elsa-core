using System.Threading.Channels;

namespace Elsa.Common.Multitenancy;

public class TenantBackgroundWorkQueue : ITenantBackgroundWorkQueue
{
    private readonly Channel<TenantBackgroundWorkItem> _channel = Channel.CreateUnbounded<TenantBackgroundWorkItem>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false
    });

    public ValueTask EnqueueAsync(TenantBackgroundWorkItem workItem, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workItem);
        return _channel.Writer.WriteAsync(workItem, cancellationToken);
    }

    public IAsyncEnumerable<TenantBackgroundWorkItem> DequeueAllAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
