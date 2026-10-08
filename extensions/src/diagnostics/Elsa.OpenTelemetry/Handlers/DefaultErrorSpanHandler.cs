using Elsa.OpenTelemetry.Contracts;
using Elsa.OpenTelemetry.Models;

namespace Elsa.OpenTelemetry.Handlers;

public class DefaultErrorSpanHandler : IActivityErrorSpanHandler, IWorkflowErrorSpanHandler
{
    public float Order => 100000;

    public bool CanHandle(WorkflowErrorSpanContext context) => false;

    public void Handle(WorkflowErrorSpanContext context)
    {
    }

    public  bool CanHandle(ActivityErrorSpanContext context) => context.Exception != null;

    public void Handle(ActivityErrorSpanContext context)
    {
        context.Span.AddException(context.Exception!);
    }
}