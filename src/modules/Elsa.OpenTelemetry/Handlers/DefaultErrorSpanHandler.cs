using Elsa.OpenTelemetry.Abstractions;
using Elsa.OpenTelemetry.Models;

namespace Elsa.OpenTelemetry.Handlers;

public class DefaultErrorSpanHandler : ErrorSpanHandlerBase
{
    public override void Handle(ErrorSpanContext context)
    {
        var span = context.Span;
        var exception = context.Exception;
        var errorMessage = string.IsNullOrWhiteSpace(exception?.Message) ? "Unknown error" : exception.Message;
        span.SetTag("error", true);
        span.SetTag("error.message", errorMessage);

        if (exception != null)
        {
            span.SetTag("error.exceptionType", exception.GetType().FullName);

            if (!string.IsNullOrEmpty(exception.StackTrace))
                span.SetTag("error.stackTrace", exception.StackTrace);
        }
    }
}