using System.Net;
using System.Threading.Tasks;
using Elsa.Workflows.Persistence.Models;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Api.ApiResults;

public class DispatchWorkflowDefinitionResult : IActionResult
{
    public DispatchWorkflowDefinitionResult(string definitionId, string? correlationId = default)
    {
        DefinitionId = definitionId;
        CorrelationId = correlationId;
    }
    
    public string DefinitionId { get; }
    public string? CorrelationId { get; set; }

    public async Task ExecuteResultAsync(ActionContext context)
    {
        var httpContext = context.HttpContext;
        var response = httpContext.Response;
        var workflowDispatcher = httpContext.RequestServices.GetRequiredService<IWorkflowDispatcher>();
        var result = await workflowDispatcher.DispatchAsync(new DispatchWorkflowDefinitionRequest(DefinitionId, VersionOptions.Published, CorrelationId: CorrelationId));

        response.StatusCode = (int)HttpStatusCode.OK;
        await response.WriteAsJsonAsync(result, httpContext.RequestAborted);
    }
}