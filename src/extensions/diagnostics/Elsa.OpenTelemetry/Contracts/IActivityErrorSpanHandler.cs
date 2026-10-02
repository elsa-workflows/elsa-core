using Elsa.OpenTelemetry.Models;

namespace Elsa.OpenTelemetry.Contracts;

public interface IActivityErrorSpanHandler
{
    float Order { get; }
    bool CanHandle(ActivityErrorSpanContext context);
    void Handle(ActivityErrorSpanContext context);
}