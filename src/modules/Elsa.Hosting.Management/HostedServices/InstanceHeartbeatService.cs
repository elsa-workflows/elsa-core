using Elsa.Common.Contracts;
using Elsa.Hosting.Management.Contracts;
using Elsa.Hosting.Management.Options;
using Elsa.KeyValues.Contracts;
using Elsa.KeyValues.Entities;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Elsa.Hosting.Management.HostedServices;

/// <summary>
/// Service to write heartbeat messages per running instance. 
/// </summary>
/// <remarks>
/// Creates a new instance of the <see cref="InstanceHeartbeatService"/>
/// </remarks>
[UsedImplicitly]
public class InstanceHeartbeatService(IServiceProvider serviceProvider, IOptions<HeartbeatOptions> heartbeatOptions) : IHostedService, IDisposable
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly HeartbeatOptions _heartbeatOptions = heartbeatOptions.Value;
    private Timer? _timer;

    internal const string HeartbeatKeyPrefix = "Heartbeat_";

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(WriteHeartbeat, null, TimeSpan.Zero, _heartbeatOptions.Interval);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, Timeout.Infinite);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose() => _timer?.Dispose();

    private void WriteHeartbeat(object? state) => _ = Task.Run(async () => await WriteHeartbeatAsync());

    private async Task WriteHeartbeatAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var instanceNameProvider = scope.ServiceProvider.GetRequiredService<IApplicationInstanceNameProvider>();
        var store = scope.ServiceProvider.GetRequiredService<IKeyValueStore>();
        var systemClock = scope.ServiceProvider.GetRequiredService<ISystemClock>();
        await store.SaveAsync(new SerializedKeyValuePair
            {
                Id = $"{HeartbeatKeyPrefix}{instanceNameProvider.GetName()}",
                SerializedValue = systemClock.UtcNow.ToString("o")
            },
            default);
    }
}