using System.Collections.Generic;
using System.Net.Mime;
using System.Threading.Tasks;
using Elsa.Activities.Http.Contracts;
using Elsa.Activities.Http.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Elsa.Activities.Http.Services;

public class DefaultHttpEndpointWorkflowFaultHandler : IHttpEndpointWorkflowFaultHandler
{
    public virtual async ValueTask HandleAsync(HttpEndpointFaultedWorkflowContext context)
    {
        var httpContext = context.HttpContext;
        var workflowInstance = context.WorkflowInstance;
        
        httpContext.Response.ContentType = MediaTypeNames.Application.Json;
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var faults = new List<object>();

        foreach (var fault in workflowInstance.Faults)
        {
            faults.Add(new
            {
                errorMessage = $"Workflow faulted at {workflowInstance.FaultedAt!} with error: {fault?.Message}",
                exception = fault?.Exception,
                workflow = new
                {
                    name = workflowInstance.Name,
                    version = workflowInstance.Version,
                    instanceId = workflowInstance.Id
                }
            });
        }        
        var faultedResponse = JsonConvert.SerializeObject(faults);

        await httpContext.Response.WriteAsync(faultedResponse, context.CancellationToken);
    }
}