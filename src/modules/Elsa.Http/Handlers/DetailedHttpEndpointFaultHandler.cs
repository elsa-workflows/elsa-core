using System.Net.Mime;
using Elsa.Http.Contracts;
using Elsa.Http.Models;
using Elsa.Workflows.Core.Contracts;
using Microsoft.AspNetCore.Http;

namespace Elsa.Http.Handlers;

/// <summary>
/// A fault handler that writes detailed information about the fault to the <see cref="HttpResponse"/>.
/// </summary>
public sealed class DetailedHttpEndpointFaultHandler : IHttpEndpointFaultHandler
{
    private readonly IApiSerializer _apiSerializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultHttpEndpointFaultHandler"/> class.
    /// </summary>
    public DetailedHttpEndpointFaultHandler(IApiSerializer apiSerializer)
    {
        _apiSerializer = apiSerializer;
    }

    /// <inheritdoc />
    public async ValueTask HandleAsync(HttpEndpointFaultContext context)
    {
        var httpContext = context.HttpContext;
        var workflowState = context.WorkflowState;
        var isTimeoutIncident = GetIsTimeoutFault(context);
        var statusCode = isTimeoutIncident ? StatusCodes.Status408RequestTimeout : StatusCodes.Status500InternalServerError;

        httpContext.Response.ContentType = MediaTypeNames.Application.Json;
        httpContext.Response.StatusCode = statusCode;

        var faultedResponse = _apiSerializer.Serialize(workflowState);
        await httpContext.Response.WriteAsync(faultedResponse, context.CancellationToken);
    }

    private bool GetIsTimeoutFault(HttpEndpointFaultContext context)
    {
        var workflowState = context.WorkflowState;
        var exceptionTypes = new[] { typeof(OperationCanceledException), typeof(TaskCanceledException), typeof(TimeoutException) };
        var timeoutIncident = workflowState.Incidents.FirstOrDefault(x => exceptionTypes.Contains(x.Exception?.Type));

        return timeoutIncident != null;
    }
}