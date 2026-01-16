using Elsa.Abstractions;
using Elsa.Common.Models;
using Elsa.Workflows.Management;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.GetByDefinitionId;

[PublicAPI]
internal class GetByDefinitionId(IWorkflowDefinitionStore store, IWorkflowDefinitionLinker linker, IMaterializerRegistry materializerRegistry) : ElsaEndpoint<Request>
{
    public override void Configure()
    {
        Get("/workflow-definitions/by-definition-id/{definitionId}", "/workflow-definitions/{definitionId}");
        ConfigurePermissions("read:workflow-definitions");
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var versionOptions = request.VersionOptions != null ? VersionOptions.FromString(request.VersionOptions) : VersionOptions.Latest;
        var filter = WorkflowDefinitionHandle.ByDefinitionId(request.DefinitionId, versionOptions).ToFilter();
        var definition = await store.FindAsync(filter, cancellationToken);

        if (definition == null)
        {
            await Send.NotFoundAsync(cancellationToken);
            return;
        }

        if (!materializerRegistry.IsMaterializerAvailable(definition.MaterializerName))
        {
            AddError($"The workflow materializer '{definition.MaterializerName}' is not available. The materializer may be disabled or not registered.");
            await Send.ErrorsAsync(StatusCodes.Status503ServiceUnavailable, cancellationToken);
            return;
        }

        var model = await linker.MapAsync(definition, cancellationToken);
        await Send.OkAsync(model, cancellationToken);
    }
}