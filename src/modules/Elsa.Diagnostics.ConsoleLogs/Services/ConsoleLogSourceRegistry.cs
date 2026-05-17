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
                var current = ApplyCurrentHealth(existing, DateTimeOffset.UtcNow);
                var updated = current with { LastSeen = timestamp, Health = ConsoleLogSourceHealth.Connected };
                if (!_sources.TryUpdate(sourceId, updated, existing))
                    continue;

                if (current.Health != updated.Health)
                    SourceChanged?.Invoke(updated);

                return;
            }

            var source = new ConsoleLogSource
            {
                Id = sourceId,
                DisplayName = sourceId,
                MachineName = "",
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
        var now = DateTimeOffset.UtcNow;
        return _sources
            .Select(entry => RefreshHealth(entry.Key, entry.Value, now))
            .OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private ConsoleLogSource RefreshHealth(string sourceId, ConsoleLogSource source, DateTimeOffset now)
    {
        var updated = ApplyCurrentHealth(source, now);
        if (updated.Health == source.Health)
            return source;

        if (!_sources.TryUpdate(sourceId, updated, source))
            return source;

        SourceChanged?.Invoke(updated);
        return updated;
    }

    private ConsoleLogSource ApplyCurrentHealth(ConsoleLogSource source, DateTimeOffset now)
    {
        var staleBefore = now.Subtract(_options.SourceHeartbeatTimeout);
        return source.LastSeen < staleBefore ? source with { Health = ConsoleLogSourceHealth.Stale } : source;
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
