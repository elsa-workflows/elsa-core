using Elsa.Http.Models;
using Microsoft.AspNetCore.Http;
using Elsa.Http.Contracts;

namespace Elsa.Http.Handlers;

/// <summary>
/// A default fault handler that writes information about the fault to the <see cref="HttpResponse"/>.
/// </summary>
public sealed class DefaultHttpEndpointFaultHandler : IHttpEndpointFaultHandler
{
    /// <inheritdoc />
    public ValueTask HandleAsync(HttpEndpointFaultContext context)
    {
        var httpContext = context.HttpContext;
        var isTimeoutIncident = GetIsTimeoutFault(context);
        var statusCode = isTimeoutIncident ? StatusCodes.Status408RequestTimeout : StatusCodes.Status500InternalServerError;
        
        httpContext.Response.StatusCode = statusCode;
        return ValueTask.CompletedTask;
    }

    private bool GetIsTimeoutFault(HttpEndpointFaultContext context)
    {
        var workflowState = context.WorkflowState;
        var exceptionTypes = new[] { typeof(OperationCanceledException), typeof(TaskCanceledException), typeof(TimeoutException) };
        var timeoutIncident = workflowState.Incidents.FirstOrDefault(x => exceptionTypes.Contains(x.Exception?.Type));

        return timeoutIncident != null;
    }
}