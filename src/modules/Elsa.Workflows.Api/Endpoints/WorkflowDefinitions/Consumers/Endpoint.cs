using Elsa.Abstractions;
using Elsa.Common.Models;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Consumers;

/// <summary>
/// Returns all workflow definitions that consume the specified workflow definition (recursively).
/// </summary>
[PublicAPI]
internal class Consumers(IWorkflowDefinitionStore store, IWorkflowReferenceGraphBuilder workflowReferenceGraphBuilder) : ElsaEndpoint<Request, Response>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Get("/workflow-definitions/{definitionId}/consumers");
        ConfigurePermissions("read:workflow-definitions");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var filter = new WorkflowDefinitionFilter
        {
            DefinitionId = request.DefinitionId,
            VersionOptions = VersionOptions.Latest
        };

        var definition = await store.FindAsync(filter, cancellationToken);

        if (definition == null)
        {
            await Send.NotFoundAsync(cancellationToken);
            return;
        }

        var graph = await workflowReferenceGraphBuilder.BuildGraphAsync(request.DefinitionId, cancellationToken);
        var consumerIds = graph.ConsumerDefinitionIds.ToList();
        await Send.OkAsync(new Response(consumerIds), cancellationToken);
    }
}
