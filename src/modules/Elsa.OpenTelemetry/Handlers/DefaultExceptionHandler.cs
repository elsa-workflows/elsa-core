using Elsa.OpenTelemetry.Contracts;
using Elsa.OpenTelemetry.Models;

namespace Elsa.OpenTelemetry.Handlers;

public class DefaultExceptionHandler : IActivityErrorSpanHandler, IWorkflowErrorSpanHandler, IErrorMetricHandler
{
    public float Order => 100000;
    public bool CanHandle(ErrorMetricContext context) => true;
    public bool CanHandle(WorkflowErrorSpanContext context) => true;
    public bool CanHandle(ActivityErrorSpanContext context) => context.Exception != null;
    
    public void Handle(WorkflowErrorSpanContext context)
    {
        // No-op.
    }
    
    public void Handle(ActivityErrorSpanContext context)
    {
        context.Span.AddException(context.Exception!);
    }
    
    public void Handle(ErrorMetricContext context)
    {
        context.Tags["error.exception.type"] = context.Exception.GetType().Name;
    }
}