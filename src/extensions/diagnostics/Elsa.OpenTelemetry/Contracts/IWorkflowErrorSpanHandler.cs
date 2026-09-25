using Elsa.OpenTelemetry.Models;

namespace Elsa.OpenTelemetry.Contracts;

public interface IWorkflowErrorSpanHandler
{
    float Order { get; }
    bool CanHandle(WorkflowErrorSpanContext context);
    void Handle(WorkflowErrorSpanContext context);
}