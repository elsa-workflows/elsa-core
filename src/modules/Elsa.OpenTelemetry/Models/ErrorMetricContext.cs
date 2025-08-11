using System.Diagnostics.Metrics;

namespace Elsa.OpenTelemetry.Models;

public class ErrorMetricContext(Counter<long> errorCounter, Exception exception, IDictionary<string, object?> tags)
{
    public Counter<long> ErrorCounter { get; } = errorCounter;
    public Exception Exception { get; } = exception;
    public IDictionary<string, object?> Tags { get; } = tags;
}