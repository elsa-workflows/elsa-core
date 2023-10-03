using Elsa.Http.Models;
using Microsoft.AspNetCore.Http;
using Elsa.Http.Contracts;
using Elsa.Http.Exceptions;

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
        var isBadRequest = GetIsBadRequestFault(context);
        var statusCode = isTimeoutIncident
            ? StatusCodes.Status408RequestTimeout
            : isBadRequest
                ? StatusCodes.Status400BadRequest
                : StatusCodes.Status500InternalServerError;

        httpContext.Response.StatusCode = statusCode;
        return ValueTask.CompletedTask;
    }

    private bool GetIsTimeoutFault(HttpEndpointFaultContext context)
    {
        return ContainsException(context, typeof(OperationCanceledException), typeof(TaskCanceledException), typeof(TimeoutException));
    }

    private bool GetIsBadRequestFault(HttpEndpointFaultContext context)
    {
        return ContainsException(context, typeof(HttpBadRequestException));
    }

    private bool ContainsException(HttpEndpointFaultContext context, params Type[] exceptionTypes)
    {
        var workflowState = context.WorkflowState;
        var timeoutIncident = workflowState.Incidents.FirstOrDefault(x => exceptionTypes.Contains(x.Exception?.Type));

        return timeoutIncident != null;
    }
}