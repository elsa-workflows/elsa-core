using Elsa.Diagnostics.OpenTelemetry.Models;

namespace Elsa.Diagnostics.OpenTelemetry.Contracts;

public interface ICollectorConfigurationProvider
{
    ValueTask<CollectorConfiguration> GetAsync(CancellationToken cancellationToken = default);
}
