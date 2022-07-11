using Elsa.AspNetCore.Attributes;
using Elsa.AspNetCore.Models;
using Elsa.Labels.Services;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Persistence.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Open.Linq.AsyncExtensions;

namespace Elsa.Labels.Endpoints.WorkflowDefinitionLabels;

[Area(AreaNames.Elsa)]
[ApiEndpoint(ControllerNames.WorkflowDefinitionLabels, "Get")]
[ProducesResponseType(typeof(ListModel<string>), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public class Get : Controller
{
    private readonly IWorkflowDefinitionStore _workflowDefinitionStore;
    private readonly IWorkflowDefinitionLabelStore _workflowDefinitionLabelStore;
    private readonly SerializerOptionsProvider _serializerOptionsProvider;

    public Get(
        IWorkflowDefinitionStore workflowDefinitionStore,
        IWorkflowDefinitionLabelStore workflowDefinitionLabelStore,
        SerializerOptionsProvider serializerOptionsProvider)
    {
        _workflowDefinitionStore = workflowDefinitionStore;
        _workflowDefinitionLabelStore = workflowDefinitionLabelStore;
        _serializerOptionsProvider = serializerOptionsProvider;
    }

    [HttpGet]
    public async Task<IActionResult> HandleAsync(string id, CancellationToken cancellationToken)
    {
        var workflowDefinition = await _workflowDefinitionStore.FindByIdAsync(id, cancellationToken);

        if (workflowDefinition == null)
            return NotFound();

        var serializerOptions = _serializerOptionsProvider.CreateApiOptions();
        var currentLabels = await _workflowDefinitionLabelStore.FindByWorkflowDefinitionVersionIdAsync(id, cancellationToken).Select(x => x.LabelId);
        var model = ListModel.Of(currentLabels);

        return Json(model, serializerOptions);
    }
}