using System.Threading;
using System.Threading.Tasks;
using Elsa.Management.Serialization;
using Elsa.Mediator.Contracts;
using Elsa.Persistence.Models;
using Elsa.Persistence.Requests;
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
        var serializerOptions = serializerOptionsProvider.CreateSerializerOptions();
        var skip = page * pageSize;
        var take = pageSize;
        var parsedVersionOptions = versionOptions != null ? VersionOptions.FromString(versionOptions) : default;
        var workflowSummaries = await requestSender.RequestAsync(new ListWorkflowSummaries(parsedVersionOptions, skip, take), cancellationToken);

        return Results.Json(workflowSummaries, serializerOptions, statusCode: StatusCodes.Status200OK);
    }
}