using Elsa.Diagnostics.OpenTelemetry.Models;

namespace Elsa.Diagnostics.OpenTelemetry.Contracts;

public interface IOpenTelemetryLiveFeed
{
    ValueTask PublishAsync(OpenTelemetryBatch batch, CancellationToken cancellationToken = default);
    IAsyncEnumerable<OpenTelemetryStreamItem> SubscribeAsync(OpenTelemetryTraceFilter filter, CancellationToken cancellationToken = default);
}
