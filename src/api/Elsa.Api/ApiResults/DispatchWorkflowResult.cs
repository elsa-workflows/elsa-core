using System.Net;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Runtime.Contracts;
using Elsa.Runtime.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Api.ApiResults;

public class DispatchWorkflowResult : IResult
{
    public DispatchWorkflowResult(Workflow workflow) => Workflow = workflow;
    public Workflow Workflow { get; }

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        var response = httpContext.Response;
        var workflowInvoker = httpContext.RequestServices.GetRequiredService<IWorkflowInvoker>();
        var (definitionId, version, _) = Workflow.Identity;
        var result = await workflowInvoker.DispatchAsync(new DispatchWorkflowDefinitionRequest(definitionId, version));

        response.StatusCode = (int)HttpStatusCode.OK;
        await response.WriteAsJsonAsync(result, httpContext.RequestAborted);
    }
}