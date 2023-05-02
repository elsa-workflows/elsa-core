using Elsa.Labels.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Filters;
using FastEndpoints;
using JetBrains.Annotations;
using Open.Linq.AsyncExtensions;

namespace Elsa.Labels.Endpoints.WorkflowDefinitionLabels.List;

[PublicAPI]
internal class List : Endpoint<Request, Response>
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
        Policies(Constants.PolicyName);
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var workflowDefinition = await _workflowDefinitionStore.FindAsync(new WorkflowDefinitionFilter { Id = request.Id }, cancellationToken);

        if (workflowDefinition == null)
        {
            await SendNotFoundAsync(cancellationToken);
            return;
        }

        var currentLabels = await _workflowDefinitionLabelStore.FindByWorkflowDefinitionVersionIdAsync(request.Id, cancellationToken).Select(x => x.LabelId);

        var response = new Response
        {
            Items = currentLabels.ToList()
        };

        await SendOkAsync(response, cancellationToken);
    }
}