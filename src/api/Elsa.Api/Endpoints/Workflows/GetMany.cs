using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Persistence.Models;
using Elsa.Persistence.Services;
using Elsa.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Api.Endpoints.Workflows;

public static partial class Workflows
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
        var workflowSummaries = await workflowDefinitionStore.FindManySummariesAsync(splitIds, parsedVersionOptions, cancellationToken);

        return Results.Json(workflowSummaries, serializerOptions, statusCode: StatusCodes.Status200OK);
    }
}