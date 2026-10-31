using Elsa.Diagnostics.OpenTelemetry.Models;

namespace Elsa.Diagnostics.OpenTelemetry.Contracts;

public interface IOpenTelemetrySourceRegistry
{
    long DroppedCount { get; }
    void MarkSeen(TelemetryResource resource);
    IReadOnlyCollection<TelemetryResource> List();
}
