using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Api.Models;
using Elsa.AspNetCore;
using Elsa.Helpers;
using Elsa.Persistence.Entities;
using Elsa.Persistence.Services;
using Elsa.Serialization;
using Elsa.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Open.Linq.AsyncExtensions;

namespace Elsa.Api.Endpoints.WorkflowDefinitionLabels;

[Area(AreaNames.Elsa)]
[ApiEndpoint(ControllerNames.WorkflowDefinitionLabels, "Update")]
[ProducesResponseType(typeof(ListModel<string>), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public class Update : Controller
{
    private readonly ILabelStore _labelStore;
    private readonly IWorkflowDefinitionStore _workflowDefinitionStore;
    private readonly IWorkflowDefinitionLabelStore _workflowDefinitionLabelStore;
    private readonly IIdentityGenerator _identityGenerator;
    private readonly WorkflowSerializerOptionsProvider _workflowSerializerOptionsProvider;

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
        IIdentityGenerator identityGenerator,
        WorkflowSerializerOptionsProvider workflowSerializerOptionsProvider)
    {
        _labelStore = labelStore;
        _workflowDefinitionStore = workflowDefinitionStore;
        _workflowDefinitionLabelStore = workflowDefinitionLabelStore;
        _identityGenerator = identityGenerator;
        _workflowSerializerOptionsProvider = workflowSerializerOptionsProvider;
    }

    [HttpPost]
    public async Task<IActionResult> HandleAsync(string id, CancellationToken cancellationToken)
    {
        var workflowDefinition = await _workflowDefinitionStore.FindByIdAsync(id, cancellationToken);

        if (workflowDefinition == null)
            return NotFound();
        
        var serializerOptions = _workflowSerializerOptionsProvider.CreateApiOptions();
        var model = (await Request.ReadFromJsonAsync<SelectedLabelsModel>(serializerOptions, cancellationToken))!;

        if (!model.LabelIds.Any())
            return NoContent();
        
        // Load the actual labels to ensure only existing ones are assigned.
        var selectedLabelIds = await _labelStore.FindManyByIdAsync(model.LabelIds, cancellationToken).Select(x => x.Id).ToList();
        
        // Project selected labels into WorkflowDefinitionLabels.
        var newLabels = selectedLabelIds.Select(x => new WorkflowDefinitionLabel
        {
            Id = _identityGenerator.GenerateId(),
            LabelId = x,
            WorkflowDefinitionId = workflowDefinition.DefinitionId,
            WorkflowDefinitionVersionId = workflowDefinition.Id
        }).ToList();
        
        // Get the current labels.
        var currentLabels = await _workflowDefinitionLabelStore.FindByWorkflowDefinitionVersionIdAsync(id, cancellationToken).ToList();
        
        // Get a diff between new labels and existing labels.
        var diff = Diff.For(currentLabels, newLabels, new WorkflowDefinitionLabelEqualityComparer());
        
        // Replace assigned labels.
        await _workflowDefinitionLabelStore.ReplaceAsync(diff.Removed, diff.Added, cancellationToken);

        var listModel = ListModel.Of(selectedLabelIds);
        return Json(listModel, serializerOptions);
    }
}