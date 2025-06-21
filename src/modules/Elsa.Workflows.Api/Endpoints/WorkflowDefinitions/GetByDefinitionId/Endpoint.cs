using Elsa.Abstractions;
using Elsa.Common.Models;
using Elsa.Workflows.Api.Models;
using Elsa.Workflows.Management;
using Elsa.Workflows.Models;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.GetByDefinitionId;

[PublicAPI]
internal class GetByDefinitionId(IWorkflowDefinitionStore store, IWorkflowDefinitionLinker linker) : ElsaEndpoint<Request, LinkedWorkflowDefinitionModel>
{
    public override void Configure()
    {
        Get("/workflow-definitions/by-definition-id/{definitionId}", "/workflow-definitions/{definitionId}");
        ConfigurePermissions("read:workflow-definitions");
    }

    public override async Task<LinkedWorkflowDefinitionModel> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        var versionOptions = request.VersionOptions != null ? VersionOptions.FromString(request.VersionOptions) : VersionOptions.Latest;
        var filter = WorkflowDefinitionHandle.ByDefinitionId(request.DefinitionId, versionOptions).ToFilter();
        var definition = await store.FindAsync(filter, cancellationToken);
        
        if (definition == null)
        {
            await SendNotFoundAsync(cancellationToken);
            return null!;
        }

        var model = await linker.MapAsync(definition, cancellationToken);
        return model;
    }
}