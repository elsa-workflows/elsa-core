using Elsa.Abstractions;
using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Serialization.Converters;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Mappers;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Count;

/// <summary>
/// An endpoint for counting workflow definitions.
/// </summary>
[PublicAPI]
internal class Count : ElsaEndpointWithoutRequest<Response>
{
    private readonly IWorkflowDefinitionStore _store;

    public Count(IWorkflowDefinitionStore store)
    {
        _store = store;
    }

    public override void Configure()
    {
        Get("/workflow-definitions/query/count");
        ConfigurePermissions("read:workflow-definitions");
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var count = await _store.CountDistinctAsync(cancellationToken);
        var response = new Response(count);
        await SendOkAsync(response, cancellationToken);
    }
}