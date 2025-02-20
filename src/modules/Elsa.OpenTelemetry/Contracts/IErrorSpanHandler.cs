using Elsa.OpenTelemetry.Abstractions;
using Elsa.OpenTelemetry.Models;

namespace Elsa.OpenTelemetry.Contracts;

public interface IErrorSpanHandler
{
    void Handle(ErrorSpanContext context);
}

