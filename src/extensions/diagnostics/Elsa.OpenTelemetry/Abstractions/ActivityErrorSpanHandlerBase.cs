using Elsa.OpenTelemetry.Contracts;
using Elsa.OpenTelemetry.Models;

namespace Elsa.OpenTelemetry.Abstractions;

public abstract class ActivityErrorSpanHandlerBase : IActivityErrorSpanHandler
{
    public virtual float Order => 0;
    public abstract bool CanHandle(ActivityErrorSpanContext context);
    public abstract void Handle(ActivityErrorSpanContext context);
}