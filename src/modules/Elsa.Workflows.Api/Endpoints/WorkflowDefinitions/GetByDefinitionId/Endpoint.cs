using Elsa.Abstractions;
using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Filters;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.GetByDefinitionId;

[PublicAPI]
internal class GetByDefinitionId(IWorkflowDefinitionStore store, IWorkflowDefinitionLinker linker) : ElsaEndpoint<Request>
{
    public override void Configure()
    {
        Get("/workflow-definitions/by-definition-id/{definitionId}", "/workflow-definitions/{definitionId}");
        ConfigurePermissions("read:workflow-definitions");
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var versionOptions = request.VersionOptions != null ? VersionOptions.FromString(request.VersionOptions) : VersionOptions.Latest;

        var filter = new WorkflowDefinitionFilter
        {
            DefinitionId = request.DefinitionId,
            VersionOptions = versionOptions
        };

        var order = new WorkflowDefinitionOrder<int>(x => x.Version, OrderDirection.Descending);
        var definition = (await store.FindManyAsync(filter, order, cancellationToken: cancellationToken)).FirstOrDefault();

        if (definition == null)
        {
            await SendNotFoundAsync(cancellationToken);
            return;
        }

        var model = await linker.MapAsync(definition, cancellationToken);
        await SendOkAsync(model, cancellationToken);
    }
}