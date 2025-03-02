using Elsa.OpenTelemetry.Abstractions;
using Elsa.OpenTelemetry.Models;
using Elsa.Workflows.Exceptions;

namespace Elsa.OpenTelemetry.Handlers;

public class FaultExceptionErrorSpanHandler : ErrorSpanHandlerBase
{
    public override void Handle(ErrorSpanContext context)
    {
        if(context.Exception is not FaultException faultException)
            return;
        
        var span = context.Span;
        span.SetTag("error.code", faultException.Code);
        span.SetTag("error.category", faultException.Category);
        span.SetTag("error.faultType", faultException.Type);
    }
}