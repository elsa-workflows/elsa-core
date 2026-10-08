using Elsa.Diagnostics.OpenTelemetry.Models;

namespace Elsa.Diagnostics.OpenTelemetry.Contracts;

public interface IOpenTelemetryIngestor
{
    ValueTask IngestAsync(OpenTelemetryBatch batch, CancellationToken cancellationToken = default);
}
