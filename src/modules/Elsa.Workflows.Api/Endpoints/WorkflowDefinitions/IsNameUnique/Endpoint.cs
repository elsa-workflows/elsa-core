using Elsa.Abstractions;
using Elsa.Workflows.Management.Contracts;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.IsNameUnique;

/// <summary>
/// Checks if a workflow definition name is unique.
/// </summary>
[PublicAPI]
internal class IsNameUnique : ElsaEndpoint<Request>
{
    private readonly IWorkflowDefinitionStore _store;

    public IsNameUnique(IWorkflowDefinitionStore store)
    {
        _store = store;
    }

    public override void Configure()
    {
        Get("/workflow-definitions/validation/is-name-unique");
        ConfigurePermissions("read:workflow-definitions");
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var exists = await _store.GetIsNameUnique(request.Name.Trim(), request.DefinitionId, cancellationToken);
        var response = new Response(!exists);
        
        await SendOkAsync(response, cancellationToken);
    }
}