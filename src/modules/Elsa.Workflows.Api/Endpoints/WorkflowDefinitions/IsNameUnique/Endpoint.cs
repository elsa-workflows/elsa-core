using Elsa.Abstractions;
using Elsa.Workflows.Management;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.IsNameUnique;

/// <summary>
/// Checks if a workflow definition name is unique.
/// </summary>
[PublicAPI]
internal class IsNameUnique(IWorkflowDefinitionStore store) : ElsaEndpoint<Request>
{
    public override void Configure()
    {
        Get("/workflow-definitions/validation/is-name-unique");
        ConfigurePermissions("read:workflow-definitions");
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var isUnique = await store.GetIsNameUnique(request.Name.Trim(), request.DefinitionId, cancellationToken);
        var response = new Response(isUnique);
        
        await Send.OkAsync(response, cancellationToken);
    }
}