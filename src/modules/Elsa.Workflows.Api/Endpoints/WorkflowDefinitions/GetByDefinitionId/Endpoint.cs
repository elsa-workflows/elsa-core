using Elsa.Abstractions;
using Elsa.Common.Models;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management.Requests;
using Elsa.Workflows.Models;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.GetByDefinitionId;

[PublicAPI]
internal class GetByDefinitionId(IMediator mediator, IWorkflowDefinitionLinker linker) : ElsaEndpoint<Request>
{
    public override void Configure()
    {
        Get("/workflow-definitions/by-definition-id/{definitionId}", "/workflow-definitions/{definitionId}");
        ConfigurePermissions("read:workflow-definitions");
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var versionOptions = request.VersionOptions != null ? VersionOptions.FromString(request.VersionOptions) : VersionOptions.Latest;
        var handle = WorkflowDefinitionHandle.ByDefinitionId(request.DefinitionId, versionOptions);
        var findRequest = new FindWorkflowDefinitionRequest(handle);
        var definition = await mediator.SendAsync(findRequest, cancellationToken);
        
        if (definition == null)
        {
            await SendNotFoundAsync(cancellationToken);
            return;
        }

        var model = await linker.MapAsync(definition, cancellationToken);
        await SendOkAsync(model, cancellationToken);
    }
}