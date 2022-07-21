using System.Threading;
using System.Threading.Tasks;
using Elsa.Persistence.Common.Models;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Persistence.Models;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Api.ApiResults;

public class ExecuteWorkflowDefinitionResult : IActionResult
{
    public ExecuteWorkflowDefinitionResult(string definitionId, string? correlationId = default)
    {
        DefinitionId = definitionId;
        CorrelationId = correlationId;
    }
    
    public string DefinitionId { get; }
    public string? CorrelationId { get; }

    public async Task ExecuteResultAsync(ActionContext context)
    {
        var httpContext = context.HttpContext;
        var response = httpContext.Response;
        var workflowInvoker = httpContext.RequestServices.GetRequiredService<IWorkflowInvoker>();
        var serializerOptionsProvider = httpContext.RequestServices.GetRequiredService<SerializerOptionsProvider>();
        var executeRequest = new InvokeWorkflowDefinitionRequest(DefinitionId, VersionOptions.Published, CorrelationId: CorrelationId);
        var result = await workflowInvoker.InvokeAsync(executeRequest, CancellationToken.None);
        var serializerOptions = serializerOptionsProvider.CreateApiOptions();

        if (!response.HasStarted)
            await response.WriteAsJsonAsync(result, serializerOptions, httpContext.RequestAborted);
    }
}