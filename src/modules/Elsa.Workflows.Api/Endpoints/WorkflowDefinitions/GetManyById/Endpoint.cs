using Elsa.Abstractions;
using Elsa.Models;
using Elsa.Workflows.Api.Models;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.GetManyById;

[PublicAPI]
internal class GetManyById(IWorkflowDefinitionStore store, IWorkflowDefinitionLinker linker) : ElsaEndpoint<Request, ListResponse<LinkedWorkflowDefinitionModel>>
{
    public override void Configure()
    {
        Get("/workflow-definitions/many-by-id");
        ConfigurePermissions("read:workflow-definitions");
    }

    public override async Task<ListResponse<LinkedWorkflowDefinitionModel>> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        var filter = new WorkflowDefinitionFilter
        {
            Ids = request.Ids
        };

        var definitions = (await store.FindManyAsync(filter, cancellationToken)).ToList();
        var models = await linker.MapAsync(definitions, cancellationToken);
        var response = new ListResponse<LinkedWorkflowDefinitionModel>(models);
        return response;
    }
}