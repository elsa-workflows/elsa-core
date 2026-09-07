using Elsa.Authorization;
using Elsa.Abstractions;
using Elsa.Workflows.Management;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Count;

/// <summary>
/// An endpoint for counting workflow definitions.
/// </summary>
[PublicAPI]
internal class Count(IWorkflowDefinitionStore store) : ElsaEndpointWithoutRequest<Response>
{
    public override void Configure()
    {
        Get("/workflow-definitions/query/count");
        RequirePermission(Elsa.Workflows.Api.Permissions.WorkflowPermissions.Definitions, CoreVerbs.View);
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var count = await store.CountDistinctAsync(cancellationToken);
        var response = new Response(count);
        await Send.OkAsync(response, cancellationToken);
    }
}