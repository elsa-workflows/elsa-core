using Elsa.Abstractions;
using Elsa.Workflows.Management;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Consumers;

/// <summary>
/// Returns all workflow definitions that consume the specified workflow definition.
/// </summary>
[PublicAPI]
internal class Consumers(IWorkflowReferenceQuery workflowReferenceQuery) : ElsaEndpoint<Request, Response>
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
        var consumerIds = (await workflowReferenceQuery.ExecuteAsync(request.DefinitionId, cancellationToken)).ToList();
        await Send.OkAsync(new Response(consumerIds), cancellationToken);
    }
}
