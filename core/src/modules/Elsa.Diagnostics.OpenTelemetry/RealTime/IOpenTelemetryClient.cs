using Elsa.Diagnostics.OpenTelemetry.Models;

namespace Elsa.Diagnostics.OpenTelemetry.RealTime;

public interface IOpenTelemetryClient
{
    Task ReceiveAsync(OpenTelemetryStreamItem item);
}
