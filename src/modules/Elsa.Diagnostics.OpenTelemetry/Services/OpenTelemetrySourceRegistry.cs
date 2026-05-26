using Elsa.Diagnostics.OpenTelemetry.Contracts;
using Elsa.Diagnostics.OpenTelemetry.Models;

namespace Elsa.Diagnostics.OpenTelemetry.Services;

public class OpenTelemetrySourceRegistry : IOpenTelemetrySourceRegistry
{
    private readonly object _lock = new();
    private readonly Dictionary<string, TelemetryResource> _resources = new(StringComparer.OrdinalIgnoreCase);

    public void MarkSeen(TelemetryResource resource)
    {
        lock (_lock)
            _resources[resource.Id] = resource;
    }

    public IReadOnlyCollection<TelemetryResource> List()
    {
        lock (_lock)
            return _resources.Values.OrderBy(x => x.ServiceName, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ToList();
    }
}
