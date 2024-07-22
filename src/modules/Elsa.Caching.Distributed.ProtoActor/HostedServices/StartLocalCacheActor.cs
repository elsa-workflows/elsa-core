using Elsa.Caching.Distributed.ProtoActor.ProtoBuf;
using Elsa.Extensions;
using Microsoft.Extensions.Hosting;
using Proto.Cluster;

namespace Elsa.Caching.Distributed.ProtoActor.HostedServices;

public class StartLocalCacheActor(Cluster cluster) : BackgroundService
{
    private LocalCacheClient? _localCacheClient;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _localCacheClient = cluster.GetLocalCacheClient();
        await _localCacheClient.Start(stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_localCacheClient != null)
            await _localCacheClient.Stop(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}