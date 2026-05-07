using System.Collections.Concurrent;
using Elsa.Diagnostics.Contracts;
using Elsa.Diagnostics.Models;
using Elsa.Diagnostics.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Diagnostics.Services;

public class ServerLogSourceRegistry : IServerLogSourceRegistry
{
    private readonly ConcurrentDictionary<string, ServerLogSource> _sources = new();
    private readonly ServerLogStreamingOptions _options;
    
    public ServerLogSourceRegistry(IOptions<ServerLogStreamingOptions> options)
    {
        _options = options.Value;
        Current = CreateCurrentSource();
        _sources[Current.Id] = Current;
    }
    
    public ServerLogSource Current { get; }
    
    public void MarkSeen(string sourceId, DateTimeOffset timestamp)
    {
        _sources.AddOrUpdate(
            sourceId,
            Current with { LastSeen = timestamp, Status = ServerLogSourceStatus.Connected },
            (_, existing) => existing with { LastSeen = timestamp, Status = ServerLogSourceStatus.Connected });
    }
    
    public IReadOnlyCollection<ServerLogSource> List()
    {
        var staleBefore = DateTimeOffset.UtcNow.Subtract(_options.SourceHeartbeatTimeout);
        return _sources.Values
            .Select(source => source.LastSeen < staleBefore ? source with { Status = ServerLogSourceStatus.Stale } : source)
            .OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
    
    private static ServerLogSource CreateCurrentSource()
    {
        var podName = Environment.GetEnvironmentVariable("HOSTNAME");
        var serviceName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? AppDomain.CurrentDomain.FriendlyName;
        var sourceId = $"{Environment.MachineName}-{Environment.ProcessId}";
        var displayName = !string.IsNullOrWhiteSpace(podName) ? podName : sourceId;
        
        return new()
        {
            Id = sourceId,
            DisplayName = displayName,
            ServiceName = serviceName,
            PodName = podName,
            Namespace = Environment.GetEnvironmentVariable("POD_NAMESPACE"),
            ContainerName = Environment.GetEnvironmentVariable("CONTAINER_NAME"),
            NodeName = Environment.GetEnvironmentVariable("NODE_NAME"),
            StartedAt = DateTimeOffset.UtcNow,
            LastSeen = DateTimeOffset.UtcNow,
            Status = ServerLogSourceStatus.Connected
        };
    }
}
