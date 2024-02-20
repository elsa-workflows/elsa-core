using Elsa.Common.Contracts;
using Elsa.Hosting.Management.Notifications;
using Elsa.Hosting.Management.Options;
using Elsa.KeyValues.Contracts;
using Elsa.KeyValues.Models;
using Elsa.Mediator.Contracts;
using JetBrains.Annotations;
using Medallion.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Elsa.Hosting.Management.HostedServices;

/// <summary>
/// Service to check the heartbeats of all running instances and determine whether instances have stopped working. 
/// </summary>
[UsedImplicitly]
public class InstanceHeartbeatMonitorService : IHostedService, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly HeartbeatOptions _heartbeatOptions;
    private Timer? _timer;

    /// <summary>
    /// Creates a new instance of the <see cref="InstanceHeartbeatMonitorService"/>
    /// </summary>
    public InstanceHeartbeatMonitorService(IServiceProvider serviceProvider, IOptions<HeartbeatOptions> heartbeatOptions)
    {
        _serviceProvider = serviceProvider;
        _heartbeatOptions = heartbeatOptions.Value;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(MonitorHeartbeats, null, TimeSpan.Zero, _heartbeatOptions.Interval);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, Timeout.Infinite);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _timer?.Dispose();
    }

    private void MonitorHeartbeats(object? state)
    {
        _ = Task.Run(async () => await MonitorHeartbeatsAsync());
    }

    private async Task MonitorHeartbeatsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var lockProvider = scope.ServiceProvider.GetRequiredService<IDistributedLockProvider>();
        var store = scope.ServiceProvider.GetRequiredService<IKeyValueStore>();
        var notificationSender = scope.ServiceProvider.GetRequiredService<INotificationSender>();
        var systemClock = scope.ServiceProvider.GetRequiredService<ISystemClock>();
        
        const string lockKey = "InstanceHeartbeatMonitorService";
        await using var monitorLock = await lockProvider.TryAcquireLockAsync(lockKey, TimeSpan.Zero);
        if (monitorLock == null)
            return;

        var filter = new KeyValueFilter
        {
            StartsWith = true,
            Key = InstanceHeartbeatService.HeartbeatKeyPrefix
        };
        var heartbeats = await store.FindManyAsync(filter, default);

        foreach (var heartbeat in heartbeats)
        {
            var lastHeartbeat = DateTimeOffset.Parse(heartbeat.SerializedValue).UtcDateTime;

            if (systemClock.UtcNow - lastHeartbeat <= _heartbeatOptions.Timeout)
                continue;

            var instanceName = heartbeat.Key[InstanceHeartbeatService.HeartbeatKeyPrefix.Length..];
            await notificationSender.SendAsync(new HeartbeatTimedOut(instanceName));
            await store.DeleteAsync(heartbeat.Key, default);
        }
    }
}