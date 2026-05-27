using Elsa.Diagnostics.OpenTelemetry.Contracts;
using Elsa.Diagnostics.OpenTelemetry.Models;
using Elsa.Diagnostics.OpenTelemetry.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Diagnostics.OpenTelemetry.Services;

public class OpenTelemetrySourceRegistry : IOpenTelemetrySourceRegistry
{
    private readonly object _lock = new();
    private readonly Dictionary<string, TelemetryResource> _resources = new(StringComparer.OrdinalIgnoreCase);
    private readonly int _capacity;
    private long _droppedCount;

    public OpenTelemetrySourceRegistry(IOptions<OpenTelemetryDiagnosticsOptions> options)
    {
        _capacity = Math.Max(1, options.Value.ResourceCapacity);
    }

    public OpenTelemetrySourceRegistry() : this(Microsoft.Extensions.Options.Options.Create(new OpenTelemetryDiagnosticsOptions()))
    {
    }

    public long DroppedCount
    {
        get
        {
            lock (_lock)
                return _droppedCount;
        }
    }

    public void MarkSeen(TelemetryResource resource)
    {
        lock (_lock)
        {
            if (!_resources.ContainsKey(resource.Id) && _resources.Count >= _capacity)
                RemoveOldestResource();

            _resources[resource.Id] = resource;
        }
    }

    public IReadOnlyCollection<TelemetryResource> List()
    {
        lock (_lock)
            return _resources.Values.OrderBy(x => x.ServiceName, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private void RemoveOldestResource()
    {
        var oldest = _resources.Values.OrderBy(x => x.LastSeen).ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase).FirstOrDefault();
        if (oldest == null)
            return;

        _resources.Remove(oldest.Id);
        _droppedCount++;
    }
}
