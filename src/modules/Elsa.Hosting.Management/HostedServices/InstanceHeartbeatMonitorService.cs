using Elsa.Hosting.Management.Notifications;
using Elsa.Hosting.Management.Options;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Models;
using Medallion.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Elsa.Hosting.Management.HostedServices;

/// <summary>
/// Service to check the heartbeats of all running instances and determine whether instances have stopped working. 
/// </summary>
public class InstanceHeartbeatMonitorService : IHostedService, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly HeartbeatSettings _heartbeatSettings;
    private Timer? _timer;

    /// <summary>
    /// Creates a new instance of the <see cref="InstanceHeartbeatService"/>
    /// </summary>
    public InstanceHeartbeatMonitorService(IServiceProvider serviceProvider, IOptions<HeartbeatSettings> heartbeatSettings)
    {
        _serviceProvider = serviceProvider;
        _heartbeatSettings = heartbeatSettings.Value;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(MonitorHeartbeats, null, TimeSpan.Zero, _heartbeatSettings.InstanceHeartbeatRhythm);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, Timeout.Infinite);
        return Task.CompletedTask;
    }

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
        
        var lockKey = "InstanceHeartbeatMonitorService";
        await using var monitorLock = await lockProvider.TryAcquireLockAsync(lockKey, TimeSpan.Zero);
        if (monitorLock == null)
            return;
        
        var filter = new KeyValueFilter { StartsWith = true, Key = InstanceHeartbeatService.HeartbeatKeyPrefix };
        var heartbeats = await store.FindManyAsync(filter, default);

        foreach (var heartbeat in heartbeats)
        {
            var lastHeartbeat = DateTimeOffset.Parse(heartbeat.SerializedValue).UtcDateTime;

            if (DateTime.UtcNow - lastHeartbeat <= _heartbeatSettings.InstanceDeactivatedPeriod)
                continue;

            var instanceName = heartbeat.Key.Substring(InstanceHeartbeatService.HeartbeatKeyPrefix.Length);
            await notificationSender.SendAsync(new InstanceDeactivated(instanceName));

            await store.DeleteAsync(heartbeat.Key, default);
        }
    }
}