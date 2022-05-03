using System.Threading;
using System.Threading.Tasks;
using Elsa.Api.ApiResults;
using Elsa.Persistence.Models;
using Elsa.Runtime.Services;
using Microsoft.AspNetCore.Http;

namespace Elsa.Api.Endpoints.WorkflowDefinitions;

public static partial class WorkflowDefinitions
{
    public static async Task<IResult> DispatchAsync(string definitionId, IWorkflowRegistry workflowRegistry, HttpResponse response, string? correlationId = default, CancellationToken cancellationToken = default)
    {
        var workflow = await workflowRegistry.FindByDefinitionIdAsync(definitionId, VersionOptions.Published, cancellationToken);
        return workflow == null ? Results.NotFound() : new DispatchWorkflowResult(workflow, correlationId);
    }
}