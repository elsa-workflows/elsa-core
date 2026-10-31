using Elsa.Diagnostics.OpenTelemetry.Models;

namespace Elsa.Diagnostics.OpenTelemetry.Contracts;

public interface IOpenTelemetryRedactor
{
    OpenTelemetryBatch Redact(OpenTelemetryBatch batch);
}
