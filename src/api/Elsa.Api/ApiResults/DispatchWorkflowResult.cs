using System.Net;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence.Models;
using Elsa.Runtime.Contracts;
using Elsa.Runtime.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Api.ApiResults;

public class DispatchWorkflowResult : IResult
{
    public DispatchWorkflowResult(Workflow workflow, string? correlationId = default)
    {
        Workflow = workflow;
        CorrelationId = correlationId;
    }

    public Workflow Workflow { get; }
    public string? CorrelationId { get; set; }

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        var response = httpContext.Response;
        var workflowDispatcher = httpContext.RequestServices.GetRequiredService<IWorkflowDispatcher>();
        var definitionId = Workflow.Identity.DefinitionId;
        var result = await workflowDispatcher.DispatchAsync(new DispatchWorkflowDefinitionRequest(definitionId, VersionOptions.Published, CorrelationId: CorrelationId));

        response.StatusCode = (int)HttpStatusCode.OK;
        await response.WriteAsJsonAsync(result, httpContext.RequestAborted);
    }
}