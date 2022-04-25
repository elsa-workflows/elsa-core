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
    public static async Task<IResult> PublishAsync(string definitionId, WorkflowSerializerOptionsProvider serializerOptionsProvider, IWorkflowRegistry workflowRegistry,
        IWorkflowPublisher workflowPublisher, CancellationToken cancellationToken)
    {
        var workflow = await workflowRegistry.FindByIdAsync(definitionId, VersionOptions.Latest, cancellationToken);
        if (workflow == null)
        {
            return Results.NotFound();
        }

        if (workflow.Publication.IsPublished)
        {
            return Results.BadRequest(new
            {
                Message = $"Workflow with id {definitionId} is already published"
            });
        }

        await workflowPublisher.PublishAsync(workflow, cancellationToken);
        var serializerOptions = serializerOptionsProvider.CreateApiOptions();
  
        return Results.Json(workflow, serializerOptions, statusCode: StatusCodes.Status202Accepted);
    }
}