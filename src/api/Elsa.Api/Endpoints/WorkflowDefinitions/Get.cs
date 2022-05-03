using System.Threading;
using System.Threading.Tasks;
using Elsa.Persistence.Models;
using Elsa.Persistence.Services;
using Elsa.Runtime.Services;
using Elsa.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Api.Endpoints.WorkflowDefinitions;

public static partial class WorkflowDefinitions
{
    public static async Task<IResult> Get(
        IWorkflowDefinitionStore workflowDefinitionStore,
        IWorkflowDefinitionService workflowDefinitionService,
        WorkflowSerializerOptionsProvider serializerOptionsProvider,
        CancellationToken cancellationToken,
        [FromRoute] string definitionId,
        [FromQuery] string? versionOptions = default)
    {
        var serializerOptions = serializerOptionsProvider.CreateApiOptions();
        var parsedVersionOptions = versionOptions != null ? VersionOptions.FromString(versionOptions) : VersionOptions.Latest;
        var definition = await workflowDefinitionStore.FindByDefinitionIdAsync(definitionId, parsedVersionOptions, cancellationToken);

        if (definition == null)
            return Results.NotFound();

        var workflow = await workflowDefinitionService.MaterializeWorkflowAsync(definition, cancellationToken);

        var model = new WorkflowDefinitionModel(
            definition.Id,
            definition.DefinitionId,
            definition.Name,
            definition.Description,
            definition.CreatedAt,
            definition.Version,
            definition.Variables,
            definition.Metadata,
            definition.ApplicationProperties,
            definition.IsLatest,
            definition.IsPublished,
            workflow.Root);

        return Results.Json(model, serializerOptions, statusCode: StatusCodes.Status200OK);
    }
}