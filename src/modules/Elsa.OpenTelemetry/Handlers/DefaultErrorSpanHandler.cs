using Elsa.OpenTelemetry.Abstractions;
using Elsa.OpenTelemetry.Models;

namespace Elsa.OpenTelemetry.Handlers;

public class DefaultErrorSpanHandler : ErrorSpanHandlerBase
{
    public override float Order => 100000;
    
    public override bool CanHandle(ErrorSpanContext context) => context.Exception != null;

    public override void Handle(ErrorSpanContext context)
    {
        context.Span.AddException(context.Exception!);
    }
}