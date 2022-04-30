using System.Threading;
using System.Threading.Tasks;
using Elsa.Management.Services;
using Elsa.Mediator.Services;
using Elsa.Persistence.Models;
using Elsa.Persistence.Requests;
using Elsa.Persistence.Services;
using Elsa.Serialization;
using Microsoft.AspNetCore.Http;

namespace Elsa.Api.Endpoints.Workflows;

public static partial class Workflows
{
    public static async Task<IResult> PublishAsync(
        string definitionId,
        WorkflowSerializerOptionsProvider serializerOptionsProvider,
        IWorkflowDefinitionStore workflowDefinitionStore,
        IWorkflowPublisher workflowPublisher,
        CancellationToken cancellationToken)
    {
        var definition = await workflowDefinitionStore.FindByDefinitionIdAsync(definitionId, VersionOptions.Latest, cancellationToken);
        
        if (definition == null)
            return Results.NotFound();

        if (definition.IsPublished)
            return Results.BadRequest(new
            {
                Message = $"Workflow with id {definitionId} is already published"
            });

        await workflowPublisher.PublishAsync(definition, cancellationToken);
        var serializerOptions = serializerOptionsProvider.CreateApiOptions();

        return Results.Json(definition, serializerOptions, statusCode: StatusCodes.Status202Accepted);
    }
}