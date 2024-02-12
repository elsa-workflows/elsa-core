using Elsa.Hosting.Management.Options;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Elsa.Hosting.Management.HostedServices;

/// <summary>
/// Service to write heartbeat messages per running instance. 
/// </summary>
public class InstanceHeartbeatService : IHostedService, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly HeartbeatSettings _heartbeatSettings;
    private Timer? _timer;

    internal static string HeartbeatKeyPrefix = "Heartbeat_";
    /// <summary>
    /// Creates a new instance of the <see cref="InstanceHeartbeatService"/>
    /// </summary>
    public InstanceHeartbeatService(IServiceProvider serviceProvider, IOptions<HeartbeatSettings> heartbeatSettings)
    {
        _serviceProvider = serviceProvider;
        _heartbeatSettings = heartbeatSettings.Value;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(WriteHeartbeat, null, TimeSpan.Zero, _heartbeatSettings.InstanceHeartbeatRhythm);
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

    private void WriteHeartbeat(object? state)
    {
        _ = Task.Run(async () => await WriteHeartbeatAsync());
    }
    
    private async Task WriteHeartbeatAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        
        var instanceNameRetriever = scope.ServiceProvider.GetRequiredService<IInstanceNameRetriever>();
        var store = scope.ServiceProvider.GetRequiredService<IKeyValueStore>();
        
        await store.SaveAsync(new SerializedKeyValuePair
            {
                Key = $"{HeartbeatKeyPrefix}{instanceNameRetriever.GetName()}",
                SerializedValue = DateTime.UtcNow.ToString("o")
            },
            default);
    }
}