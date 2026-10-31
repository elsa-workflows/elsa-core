using Elsa.Diagnostics.OpenTelemetry.Contracts;
using Elsa.Diagnostics.OpenTelemetry.Models;

namespace Elsa.Diagnostics.OpenTelemetry.Services;

public class OpenTelemetryIngestor(IOpenTelemetryRedactor redactor, IOpenTelemetryStore store, IOpenTelemetryLiveFeed liveFeed) : IOpenTelemetryIngestor
{
    public async ValueTask IngestAsync(OpenTelemetryBatch batch, CancellationToken cancellationToken = default)
    {
        var redactedBatch = redactor.Redact(batch);
        await store.WriteAsync(redactedBatch, cancellationToken);
        await liveFeed.PublishAsync(redactedBatch, cancellationToken);
    }
}
