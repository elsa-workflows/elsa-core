using Elsa.OpenTelemetry.Abstractions;
using Elsa.OpenTelemetry.Models;
using Elsa.Workflows.Exceptions;

namespace Elsa.OpenTelemetry.Handlers;

public class FaultExceptionErrorSpanHandler : ErrorSpanHandlerBase
{
    public override void Handle(ErrorSpanContext context)
    {
        if (context.Exception is not FaultException faultException)
            return;

        var span = context.Span;
        var tags = new Dictionary<string, object?>
        {
            ["exception.code"] = faultException.Code,
            ["exception.category"] = faultException.Category,
            ["exception.type"] = faultException.Type
        };
        span.AddException(context.Exception, new(tags.ToArray()));
    }
}