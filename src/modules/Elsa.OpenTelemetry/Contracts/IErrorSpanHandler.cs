using Elsa.OpenTelemetry.Models;

namespace Elsa.OpenTelemetry.Contracts;

public interface IErrorSpanHandler
{
    float Order { get; }
    bool CanHandle(ErrorSpanContext context);
    void Handle(ErrorSpanContext context);
}

