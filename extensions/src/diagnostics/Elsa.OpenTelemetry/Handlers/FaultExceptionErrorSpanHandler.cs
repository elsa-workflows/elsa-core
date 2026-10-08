using Elsa.OpenTelemetry.Contracts;
using Elsa.OpenTelemetry.Models;
using Elsa.Workflows.Exceptions;

namespace Elsa.OpenTelemetry.Handlers;

public class FaultExceptionErrorSpanHandler : IActivityErrorSpanHandler, IWorkflowErrorSpanHandler
{
    public float Order => 0;

    public bool CanHandle(ActivityErrorSpanContext context) => context.Exception is FaultException;
    public bool CanHandle(WorkflowErrorSpanContext context) => context.Exception is FaultException;

    public void Handle(ActivityErrorSpanContext context)
    {
        var faultException = (FaultException)context.Exception!;
        var span = context.Span;
        var tags = new Dictionary<string, object?>()
        {
            ["exception.code"] = faultException.Code,
            ["exception.category"] = faultException.Category,
            ["exception.type"] = faultException.Type
        };
        span.AddException(faultException, new(tags.ToArray()));
    }

    public void Handle(WorkflowErrorSpanContext context)
    {
        var faultException = (FaultException)context.Exception!;
        var span = context.Span;
        
        // The following two attributes are well-known by datadog.
        span.SetTag("error.code", faultException.Code);
        span.SetTag("error.category", faultException.Category);
        
        // Datadog will ignore unknown attributes, so we'll set them on a different object.
        span.SetTag("error_details.type", faultException.Type);
    }
}