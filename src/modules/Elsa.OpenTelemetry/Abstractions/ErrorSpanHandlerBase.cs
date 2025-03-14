using Elsa.OpenTelemetry.Contracts;
using Elsa.OpenTelemetry.Models;

namespace Elsa.OpenTelemetry.Abstractions;

public abstract class ErrorSpanHandlerBase : IErrorSpanHandler
{
    public virtual float Order => 0;
    public abstract bool CanHandle(ErrorSpanContext context);
    public abstract void Handle(ErrorSpanContext context);
}