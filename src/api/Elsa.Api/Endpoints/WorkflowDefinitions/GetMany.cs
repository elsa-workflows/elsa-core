using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Persistence.Models;
using Elsa.Persistence.Services;
using Elsa.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Api.Endpoints.WorkflowDefinitions;

public static partial class WorkflowDefinitions
{
    public static async Task<IResult> GetManyAsync(
        IWorkflowDefinitionStore workflowDefinitionStore,
        WorkflowSerializerOptionsProvider serializerOptionsProvider,
        CancellationToken cancellationToken,
        [FromQuery] string? definitionIds = default,
        [FromQuery] string? versionOptions = default)
    {
        var serializerOptions = serializerOptionsProvider.CreateApiOptions();
        var splitIds = definitionIds?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
        var parsedVersionOptions = versionOptions != null ? VersionOptions.FromString(versionOptions) : VersionOptions.Latest;
        var definitionSummaries = await workflowDefinitionStore.FindManySummariesAsync(splitIds, parsedVersionOptions, cancellationToken);

        return Results.Json(definitionSummaries, serializerOptions, statusCode: StatusCodes.Status200OK);
    }
}