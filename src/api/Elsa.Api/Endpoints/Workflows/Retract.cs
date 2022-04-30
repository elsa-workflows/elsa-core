using System.Threading;
using System.Threading.Tasks;
using Elsa.Management.Services;
using Elsa.Mediator.Services;
using Elsa.Persistence.Models;
using Elsa.Persistence.Requests;
using Elsa.Persistence.Services;
using Elsa.Runtime.Services;
using Elsa.Serialization;
using Microsoft.AspNetCore.Http;

namespace Elsa.Api.Endpoints.Workflows;

public static partial class Workflows
{
    public static async Task<IResult> RetractAsync(
        string definitionId, 
        WorkflowSerializerOptionsProvider serializerOptionsProvider, 
        IWorkflowDefinitionStore workflowDefinitionStore,
        IWorkflowPublisher workflowPublisher,
        IWorkflowDefinitionService workflowDefinitionService,
        CancellationToken cancellationToken)
    {
        var definition = await workflowDefinitionStore.FindByDefinitionIdAsync(definitionId, VersionOptions.LatestOrPublished, cancellationToken);
        
        if (definition == null)
            return Results.NotFound();

        await workflowPublisher.RetractAsync(definition, cancellationToken);
        var serializerOptions = serializerOptionsProvider.CreateApiOptions();
        var workflow = await workflowDefinitionService.MaterializeWorkflowAsync(definition, cancellationToken);
  
        return Results.Json(workflow, serializerOptions, statusCode: StatusCodes.Status202Accepted);
    }
}