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
    public static async Task<IResult> Get(
        IRequestSender requestSender,
        WorkflowSerializerOptionsProvider serializerOptionsProvider,
        CancellationToken cancellationToken,
        [FromRoute] string definitionId,
        [FromQuery] string? versionOptions = default)
    {
        var serializerOptions = serializerOptionsProvider.CreateApiOptions();
        var parsedVersionOptions = versionOptions != null ? VersionOptions.FromString(versionOptions) : VersionOptions.Latest;
        var workflow = await requestSender.RequestAsync(new FindWorkflowDefinitionByDefinitionId(definitionId, parsedVersionOptions), cancellationToken);
        return workflow == null ? Results.NotFound() : Results.Json(workflow, serializerOptions, statusCode: StatusCodes.Status200OK);
    }
}