using Elsa.OpenTelemetry.Abstractions;
using Elsa.OpenTelemetry.Models;

namespace Elsa.OpenTelemetry.Handlers;

public class DefaultErrorSpanHandler : ErrorSpanHandlerBase
{
    public override void Handle(ErrorSpanContext context)
    {
        if (context.Exception is null)
            return;
        
        context.Span.AddException(context.Exception);
    }
}