using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace Elsa.Diagnostics.ConsoleLogs.Services;

public class ConsoleLogSourceRegistry : IConsoleLogSourceRegistry
{
    private readonly ConcurrentDictionary<string, ConsoleLogSource> _sources = new();
    private readonly ConsoleLogsOptions _options;

    public ConsoleLogSourceRegistry(IOptions<ConsoleLogsOptions> options)
    {
        _options = options.Value;
        Current = CreateCurrentSource();
        _sources[Current.Id] = Current;
    }

    public event Action<ConsoleLogSource>? SourceChanged;

    public ConsoleLogSource Current { get; }

    public void MarkSeen(string sourceId, DateTimeOffset timestamp)
    {
        while (true)
        {
            if (_sources.TryGetValue(sourceId, out var existing))
            {
                var updated = existing with { LastSeen = timestamp, Health = ConsoleLogSourceHealth.Connected };
                if (!_sources.TryUpdate(sourceId, updated, existing))
                    continue;

                if (existing.Health != updated.Health)
                    SourceChanged?.Invoke(updated);

                return;
            }

            var source = new ConsoleLogSource
            {
                Id = sourceId,
                DisplayName = sourceId,
                MachineName = sourceId,
                ProcessId = 0,
                LastSeen = timestamp,
                Health = ConsoleLogSourceHealth.Connected
            };

            if (!_sources.TryAdd(sourceId, source))
                continue;

            SourceChanged?.Invoke(source);
            return;
        }
    }

    public IReadOnlyCollection<ConsoleLogSource> List()
    {
        var staleBefore = DateTimeOffset.UtcNow.Subtract(_options.SourceHeartbeatTimeout);
        return _sources.Values
            .Select(source => source.LastSeen < staleBefore ? source with { Health = ConsoleLogSourceHealth.Stale } : source)
            .OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static ConsoleLogSource CreateCurrentSource()
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
            Health = ConsoleLogSourceHealth.Connected
        };
    }
}
