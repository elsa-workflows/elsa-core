using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Mediator.Contracts;
using Elsa.Persistence.Models;
using Elsa.Persistence.Requests;
using Elsa.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Api.Endpoints.Workflows;

public static partial class Workflows
{
    public static async Task<IResult> GetManyAsync(
        IRequestSender requestSender,
        WorkflowSerializerOptionsProvider serializerOptionsProvider,
        CancellationToken cancellationToken,
        [FromQuery] string? definitionIds = default,
        [FromQuery] string? versionOptions = default)
    {
        var serializerOptions = serializerOptionsProvider.CreateApiOptions();
        var splitIds = definitionIds?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
        var parsedVersionOptions = versionOptions != null ? VersionOptions.FromString(versionOptions) : VersionOptions.Latest;
        var workflowSummaries = await requestSender.RequestAsync(new FindManyWorkflowDefinitions(splitIds, parsedVersionOptions), cancellationToken);

        return Results.Json(workflowSummaries, serializerOptions, statusCode: StatusCodes.Status200OK);
    }
}