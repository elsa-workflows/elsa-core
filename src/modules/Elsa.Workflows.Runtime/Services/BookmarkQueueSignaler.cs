using System.Collections.Concurrent;
using System.Threading.Channels;
using Elsa.Common.Multitenancy;

namespace Elsa.Workflows.Runtime;

public class BookmarkQueueSignaler : IBookmarkQueueSignaler
{
    private readonly ConcurrentDictionary<string, Channel<object?>> _channels = new();
    private readonly ITenantAccessor? _tenantAccessor;

    public BookmarkQueueSignaler()
    {
    }

    public BookmarkQueueSignaler(ITenantAccessor tenantAccessor)
    {
        _tenantAccessor = tenantAccessor;
    }

    public Task AwaitAsync(CancellationToken cancellationToken = default)
    {
        return GetChannel().Reader.ReadAsync(cancellationToken).AsTask();
    }

    public Task TriggerAsync(CancellationToken cancellationToken = default)
    {
        return TriggerAsync(CurrentTenantId(), cancellationToken);
    }

    public Task TriggerAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        GetChannel(tenantId).Writer.TryWrite(null);
        return Task.CompletedTask;
    }

    public void Release(string tenantId)
    {
        _channels.TryRemove(tenantId.NormalizeTenantId(), out _);
    }

    private Channel<object?> GetChannel(string? tenantId = null)
    {
        return _channels.GetOrAdd((tenantId ?? CurrentTenantId()).NormalizeTenantId(), static _ => CreateChannel());
    }

    private string CurrentTenantId() => (_tenantAccessor?.TenantId).NormalizeTenantId();

    private static Channel<object?> CreateChannel()
    {
        var options = new BoundedChannelOptions(1)
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        };
        return Channel.CreateBounded<object?>(options);
    }
}
