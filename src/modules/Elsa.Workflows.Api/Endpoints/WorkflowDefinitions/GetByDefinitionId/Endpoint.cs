using Elsa.Abstractions;
using Elsa.Common.Models;
using Elsa.Common.Multitenancy;
using Elsa.Workflows.Management;
using Elsa.Workflows.Models;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.GetByDefinitionId;

[PublicAPI]
internal class GetByDefinitionId(IWorkflowDefinitionStore store, IWorkflowDefinitionLinker linker, ITenantAccessor tenantAccessor) : ElsaEndpoint<Request>
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
        filter.TenantId = tenantAccessor.Tenant?.Id;
        var definition = await store.FindAsync(filter, cancellationToken);
        
        if (definition == null)
        {
            await SendNotFoundAsync(cancellationToken);
            return;
        }

        var model = await linker.MapAsync(definition, cancellationToken);
        await SendOkAsync(model, cancellationToken);
    }
}