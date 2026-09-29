using Elsa.OpenTelemetry.Contracts;
using Elsa.OpenTelemetry.Models;

namespace Elsa.OpenTelemetry.Abstractions;

public abstract class WorkflowErrorSpanHandlerBase : IWorkflowErrorSpanHandler
{
    public virtual float Order => 0;
    public abstract bool CanHandle(WorkflowErrorSpanContext context);
    public abstract void Handle(WorkflowErrorSpanContext context);
}