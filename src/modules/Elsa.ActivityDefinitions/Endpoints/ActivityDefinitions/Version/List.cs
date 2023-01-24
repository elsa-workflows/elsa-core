using Elsa.ActivityDefinitions.Services;
using Elsa.Common.Models;
using FastEndpoints;
using Microsoft.AspNetCore.Http;

namespace Elsa.ActivityDefinitions.Endpoints.ActivityDefinitions.Version;

public class ListVersions : EndpointWithoutRequest
{
    private readonly IActivityDefinitionStore _store;
    
    public ListVersions(IActivityDefinitionStore store)
    {
        _store = store;
    }

    public override void Configure()
    {
        Get("activity-definitions/{definitionId}/versions");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var definitionId = Route<string>("definitionId")!;

        var result = await _store.FindManyByDefinitionIdAsync(definitionId, VersionOptions.All, ct);
        if (!result.Any())
        {
            await SendNotFoundAsync(ct);
            return;
        }

        await SendAsync(result, StatusCodes.Status200OK, ct);
    }
}