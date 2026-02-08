using Elsa.Abstractions;
using Elsa.Labels.Contracts;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using JetBrains.Annotations;
using Open.Linq.AsyncExtensions;

namespace Elsa.Labels.Endpoints.WorkflowDefinitionLabels.List;

[PublicAPI]
internal class List : ElsaEndpoint<Request, Response>
{
    private readonly IWorkflowDefinitionStore _workflowDefinitionStore;
    private readonly IWorkflowDefinitionLabelStore _workflowDefinitionLabelStore;

    public List(
        IWorkflowDefinitionStore workflowDefinitionStore,
        IWorkflowDefinitionLabelStore workflowDefinitionLabelStore)
    {
        _workflowDefinitionStore = workflowDefinitionStore;
        _workflowDefinitionLabelStore = workflowDefinitionLabelStore;
    }

    public override void Configure()
    {
        Get("/workflow-definitions/{id}/labels");
        ConfigurePermissions("read:workflow-definition-labels");
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var workflowDefinition = await _workflowDefinitionStore.FindAsync(new WorkflowDefinitionFilter { Id = request.Id }, cancellationToken);

        if (workflowDefinition == null)
        {
            await Send.NotFoundAsync(cancellationToken);
            return;
        }

        var currentLabels = await _workflowDefinitionLabelStore.FindByWorkflowDefinitionVersionIdAsync(request.Id, cancellationToken).Select(x => x.LabelId);

        var response = new Response
        {
            Items = currentLabels.ToList()
        };

        await Send.OkAsync(response, cancellationToken);
    }
}