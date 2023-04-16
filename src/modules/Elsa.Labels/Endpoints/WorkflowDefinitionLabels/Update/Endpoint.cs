using Elsa.Labels.Contracts;
using Elsa.Labels.Entities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Management.Contracts;
using FastEndpoints;
using JetBrains.Annotations;
using Open.Linq.AsyncExtensions;

namespace Elsa.Labels.Endpoints.WorkflowDefinitionLabels.Update;

[PublicAPI]
internal class Update : Endpoint<Request, Response>
{
    private readonly ILabelStore _labelStore;
    private readonly IWorkflowDefinitionStore _workflowDefinitionStore;
    private readonly IWorkflowDefinitionLabelStore _workflowDefinitionLabelStore;
    private readonly IIdentityGenerator _identityGenerator;

    // ReSharper disable once ClassNeverInstantiated.Local
    private record SelectedLabelsModel(ICollection<string> LabelIds);

    private class WorkflowDefinitionLabelEqualityComparer : IEqualityComparer<WorkflowDefinitionLabel>
    {
        public bool Equals(WorkflowDefinitionLabel? x, WorkflowDefinitionLabel? y) => x?.LabelId.Equals(y?.LabelId) ?? false;
        public int GetHashCode(WorkflowDefinitionLabel obj) => obj.LabelId.GetHashCode();
    }

    public Update(
        ILabelStore labelStore, 
        IWorkflowDefinitionStore workflowDefinitionStore, 
        IWorkflowDefinitionLabelStore workflowDefinitionLabelStore, 
        IIdentityGenerator identityGenerator)
    {
        _labelStore = labelStore;
        _workflowDefinitionStore = workflowDefinitionStore;
        _workflowDefinitionLabelStore = workflowDefinitionLabelStore;
        _identityGenerator = identityGenerator;
    }

    public override void Configure()
    {
        Post("/workflow-definitions/{id}/labels");
        Policies(Constants.PolicyName);
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var workflowDefinition = await _workflowDefinitionStore.FindAsync(new WorkflowDefinitionFilter{ Id = request.Id}, cancellationToken);

        if (workflowDefinition == null)
        {
            await SendNotFoundAsync(cancellationToken);
            return;
        }

        // Load the actual labels to ensure only existing ones are assigned.
        var selectedLabelIds = await _labelStore.FindManyByIdAsync(request.LabelIds, cancellationToken).Select(x => x.Id).ToList();
        
        // Project selected labels into WorkflowDefinitionLabels.
        var newLabels = selectedLabelIds.Select(x => new WorkflowDefinitionLabel
        {
            Id = _identityGenerator.GenerateId(),
            LabelId = x,
            WorkflowDefinitionId = workflowDefinition.DefinitionId,
            WorkflowDefinitionVersionId = workflowDefinition.Id
        }).ToList();
        
        // Get the current labels.
        var currentLabels = await _workflowDefinitionLabelStore.FindByWorkflowDefinitionVersionIdAsync(request.Id, cancellationToken).ToList();
        
        // Get a diff between new labels and existing labels.
        var diff = Diff.For(currentLabels, newLabels, new WorkflowDefinitionLabelEqualityComparer());
        
        // Replace assigned labels.
        await _workflowDefinitionLabelStore.ReplaceAsync(diff.Removed, diff.Added, cancellationToken);

        var response = new Response(selectedLabelIds);
        await SendOkAsync(response, cancellationToken);
    }

}