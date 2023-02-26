using Elsa.Common.Models;
using Elsa.Workflows.Management.Services;
using FastEndpoints;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Version;

[PublicAPI]
internal class ListVersions : EndpointWithoutRequest
{
    private readonly IWorkflowDefinitionStore _store;
    
    public ListVersions(IWorkflowDefinitionStore store)
    {
        _store = store;
    }

    public override void Configure()
    {
        Get("workflow-definitions/{definitionId}/versions");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var definitionId = Route<string>("definitionId")!;
        
        var filter = new WorkflowDefinitionFilter
        {
            DefinitionId = definitionId,
            VersionOptions = VersionOptions.All
        };
        
        var definitions = await _store.FindManyAsync(filter, cancellationToken);
        
        if (!definitions.Any())
        {
            await SendNotFoundAsync(cancellationToken);
            return;
        }

        await SendAsync(definitions, StatusCodes.Status200OK, cancellationToken);
    }
}