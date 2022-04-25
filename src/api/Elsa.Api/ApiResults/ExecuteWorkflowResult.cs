using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence.Models;
using Elsa.Runtime.Models;
using Elsa.Runtime.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Api.ApiResults;

public class ExecuteWorkflowResult : IResult
{
    public ExecuteWorkflowResult(Workflow workflow) => Workflow = workflow;
    public Workflow Workflow { get; }

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        var response = httpContext.Response;
        var workflowInvoker = httpContext.RequestServices.GetRequiredService<IWorkflowInvoker>();
        var definitionId = Workflow.Identity.DefinitionId;
        var executeRequest = new InvokeWorkflowDefinitionRequest(definitionId, VersionOptions.Published);
        var result = await workflowInvoker.InvokeAsync(executeRequest, CancellationToken.None);

        if (!response.HasStarted)
            await response.WriteAsJsonAsync(result, httpContext.RequestAborted);
    }
}