using System.Threading;
using System.Threading.Tasks;
using Elsa.Mediator.Services;
using Elsa.Persistence.Models;
using Elsa.Persistence.Requests;
using Elsa.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Api.Endpoints.Workflows;

public static partial class Workflows
{
    public static async Task<IResult> ListAsync(
        IRequestSender requestSender,
        WorkflowSerializerOptionsProvider serializerOptionsProvider,
        CancellationToken cancellationToken,
        [FromQuery] string? versionOptions,
        [FromQuery] int? page,
        [FromQuery] int? pageSize)
    {
        var serializerOptions = serializerOptionsProvider.CreateApiOptions();
        var skip = page * pageSize;
        var take = pageSize;
        var parsedVersionOptions = versionOptions != null ? VersionOptions.FromString(versionOptions) : default;
        var workflowSummaries = await requestSender.RequestAsync(new ListWorkflowSummaries(parsedVersionOptions, skip, take), cancellationToken);

        return Results.Json(workflowSummaries, serializerOptions, statusCode: StatusCodes.Status200OK);
    }
}