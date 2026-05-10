using System.Collections.Concurrent;
using Elsa.Diagnostics.StructuredLogs.Contracts;
using Elsa.Diagnostics.StructuredLogs.Models;
using Elsa.Diagnostics.StructuredLogs.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Diagnostics.StructuredLogs.Services;

public class StructuredLogSourceRegistry : IStructuredLogSourceRegistry
{
    private readonly ConcurrentDictionary<string, StructuredLogSource> _sources = new();
    private readonly StructuredLogsOptions _options;

    public StructuredLogSourceRegistry(IOptions<StructuredLogsOptions> options)
    {
        _options = options.Value;
        Current = CreateCurrentSource();
        _sources[Current.Id] = Current;
    }

    public event Action<StructuredLogSource>? SourceChanged;

    public StructuredLogSource Current { get; }

    public void MarkSeen(string sourceId, DateTimeOffset timestamp)
    {
        while (true)
        {
            if (_sources.TryGetValue(sourceId, out var existing))
            {
                var updated = existing with { LastSeen = timestamp, Status = StructuredLogSourceStatus.Connected };
                if (!_sources.TryUpdate(sourceId, updated, existing))
                    continue;

                if (existing.Status != updated.Status)
                    SourceChanged?.Invoke(updated);

                return;
            }

            var source = new StructuredLogSource
            {
                Id = sourceId,
                DisplayName = sourceId,
                MachineName = sourceId,
                ProcessId = 0,
                LastSeen = timestamp,
                Status = StructuredLogSourceStatus.Connected
            };

            if (!_sources.TryAdd(sourceId, source))
                continue;

            SourceChanged?.Invoke(source);
            return;
        }
    }

    public IReadOnlyCollection<StructuredLogSource> List()
    {
        var staleBefore = DateTimeOffset.UtcNow.Subtract(_options.SourceHeartbeatTimeout);
        return _sources.Values
            .Select(source => source.LastSeen < staleBefore ? source with { Status = StructuredLogSourceStatus.Stale } : source)
            .OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static StructuredLogSource CreateCurrentSource()
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
            Status = StructuredLogSourceStatus.Connected
        };
    }
}
