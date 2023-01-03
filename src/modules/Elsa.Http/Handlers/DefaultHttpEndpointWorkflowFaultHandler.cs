using System.Net.Mime;
using System.Text.Json;
using Elsa.Http.Models;
using Elsa.Http.Services;
using Microsoft.AspNetCore.Http;

namespace Elsa.Http.Handlers;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
/// <summary>
/// A default fault handler that writes information about the fault to the <see cref="HttpResponse"/>.
/// </summary>
public class DefaultHttpEndpointWorkflowFaultHandler : IHttpEndpointWorkflowFaultHandler
{
    /// <inheritdoc />
    public virtual async ValueTask HandleAsync(HttpEndpointFaultedWorkflowContext context)
    {
        var httpContext = context.HttpContext;
        var workflowInstance = context.WorkflowInstance;
        var fault = workflowInstance.WorkflowState.Fault!;

        httpContext.Response.ContentType = MediaTypeNames.Application.Json;
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var faultedResponse = JsonSerializer.Serialize(new
        {
            errorMessage = $"Workflow faulted at {workflowInstance.FaultedAt!} with error: {fault.Message}",
            exception = fault.Exception,
            workflow = new
            {
                name = workflowInstance.Name,
                version = workflowInstance.Version,
                instanceId = workflowInstance.Id
            }
        });

        await httpContext.Response.WriteAsync(faultedResponse, context.CancellationToken);
    }
}