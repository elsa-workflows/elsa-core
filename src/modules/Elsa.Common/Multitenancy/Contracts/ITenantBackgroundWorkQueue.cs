namespace Elsa.Common.Multitenancy;

public delegate Task TenantBackgroundWorkItem(IServiceProvider serviceProvider, CancellationToken cancellationToken);

public interface ITenantBackgroundWorkQueue
{
    ValueTask EnqueueAsync(TenantBackgroundWorkItem workItem, CancellationToken cancellationToken = default);
    IAsyncEnumerable<TenantBackgroundWorkItem> DequeueAllAsync(CancellationToken cancellationToken = default);
}
