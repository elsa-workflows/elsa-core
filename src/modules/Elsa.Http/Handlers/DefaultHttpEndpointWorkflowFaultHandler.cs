using Elsa.Http.Models;
using Microsoft.AspNetCore.Http;
using System.Net.Mime;
using Elsa.Http.Contracts;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.Http.Handlers;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
/// <summary>
/// A default fault handler that writes information about the fault to the <see cref="HttpResponse"/>.
/// </summary>
public class DefaultHttpEndpointWorkflowFaultHandler : IHttpEndpointWorkflowFaultHandler
{
    private readonly IApiSerializer _apiSerializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultHttpEndpointWorkflowFaultHandler"/> class.
    /// </summary>
    public DefaultHttpEndpointWorkflowFaultHandler(IApiSerializer apiSerializer)
    {
        _apiSerializer = apiSerializer;
    }

    /// <inheritdoc />
    public virtual async ValueTask HandleAsync(HttpEndpointFaultedWorkflowContext context)
    {
        var httpContext = context.HttpContext;
        var workflowState = context.WorkflowState;
        var fault = workflowState.Fault!;

        httpContext.Response.ContentType = MediaTypeNames.Application.Json;
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var faultedResponse = _apiSerializer.Serialize(new
        {
            errorMessage = $"Workflow faulted with error: {fault.Message}",
            workflowState = workflowState
        });

        await httpContext.Response.WriteAsync(faultedResponse, context.CancellationToken);
    }
}