using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Elsa.Diagnostics.Contracts;
using Elsa.Diagnostics.Models;
using Elsa.Diagnostics.Options;
using Elsa.Diagnostics.Services;
using Microsoft.Extensions.Options;

namespace Elsa.Diagnostics.Providers.InMemory;

public class InMemoryServerLogProvider : IServerLogProvider
{
    private readonly RingBuffer<ServerLogEvent> _recentLogs;
    private readonly IServerLogSourceRegistry _sourceRegistry;
    private readonly ServerLogStreamingOptions _options;
    private readonly object _subscribersLock = new();
    private readonly Dictionary<Guid, Channel<ServerLogEvent>> _subscribers = new();
    
    public InMemoryServerLogProvider(IOptions<ServerLogStreamingOptions> options, IServerLogSourceRegistry sourceRegistry)
    {
        _options = options.Value;
        _sourceRegistry = sourceRegistry;
        _recentLogs = new(_options.RecentLogCapacity);
    }
    
    public ValueTask PublishAsync(ServerLogEvent logEvent, CancellationToken cancellationToken = default)
    {
        _recentLogs.Add(logEvent);
        _sourceRegistry.MarkSeen(logEvent.SourceId, logEvent.ReceivedAt);
        
        List<Channel<ServerLogEvent>> subscribers;
        lock (_subscribersLock)
            subscribers = _subscribers.Values.ToList();
        
        foreach (var subscriber in subscribers)
            subscriber.Writer.TryWrite(logEvent);
        
        return ValueTask.CompletedTask;
    }
    
    public ValueTask<RecentServerLogsResult> GetRecentAsync(ServerLogFilter filter, CancellationToken cancellationToken = default)
    {
        var take = Math.Clamp(filter.Take ?? _options.MaxRecentLogQuerySize, 0, _options.MaxRecentLogQuerySize);
        var items = _recentLogs
            .Snapshot()
            .Where(x => ServerLogFilterEvaluator.Matches(x, filter))
            .OrderBy(x => x.Timestamp)
            .ThenBy(x => x.Sequence)
            .TakeLast(take)
            .ToList();
        
        return ValueTask.FromResult(new RecentServerLogsResult(items, _recentLogs.DroppedCount));
    }
    
    public async IAsyncEnumerable<ServerLogEvent> SubscribeAsync(ServerLogFilter filter, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var subscriberId = Guid.NewGuid();
        var channel = Channel.CreateBounded<ServerLogEvent>(new BoundedChannelOptions(_options.SubscriberChannelCapacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });
        
        lock (_subscribersLock)
            _subscribers[subscriberId] = channel;
        
        try
        {
            await foreach (var logEvent in channel.Reader.ReadAllAsync(cancellationToken))
            {
                if (ServerLogFilterEvaluator.Matches(logEvent, filter))
                    yield return logEvent;
            }
        }
        finally
        {
            lock (_subscribersLock)
                _subscribers.Remove(subscriberId);
        }
    }
    
    public ValueTask<IReadOnlyCollection<ServerLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(_sourceRegistry.List());
    }
}
