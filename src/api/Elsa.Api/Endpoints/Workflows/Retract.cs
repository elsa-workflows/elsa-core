using System.Threading;
using System.Threading.Tasks;
using Elsa.Management.Services;
using Elsa.Persistence.Models;
using Elsa.Runtime.Services;
using Elsa.Serialization;
using Microsoft.AspNetCore.Http;

namespace Elsa.Api.Endpoints.Workflows;

public static partial class Workflows
{
    public static async Task<IResult> RetractAsync(string definitionId, WorkflowSerializerOptionsProvider serializerOptionsProvider, IWorkflowRegistry workflowRegistry,
        IWorkflowPublisher workflowPublisher, CancellationToken cancellationToken)
    {
        var workflow = await workflowRegistry.FindByIdAsync(definitionId, VersionOptions.LatestOrPublished, cancellationToken);
        if (workflow == null)
        {
            return Results.NotFound();
        }

        await workflowPublisher.RetractAsync(workflow, cancellationToken);
        var serializerOptions = serializerOptionsProvider.CreateApiOptions();
  
        return Results.Json(workflow, serializerOptions, statusCode: StatusCodes.Status202Accepted);
    }
}