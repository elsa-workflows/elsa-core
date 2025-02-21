using Elsa.OpenTelemetry.Contracts;
using Elsa.OpenTelemetry.Models;

namespace Elsa.OpenTelemetry.Abstractions;

public abstract class ErrorSpanHandlerBase : IErrorSpanHandler
{
    public abstract void Handle(ErrorSpanContext context);
}