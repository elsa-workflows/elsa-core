using Elsa.OpenTelemetry.Models;

namespace Elsa.OpenTelemetry.Contracts;

public interface IErrorMetricHandler
{
    float Order { get; }
    bool CanHandle(ErrorMetricContext context);
    void Handle(ErrorMetricContext context);
}