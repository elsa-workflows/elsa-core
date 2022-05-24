using Elsa.AspNetCore;
using Elsa.Labels.Entities;
using Elsa.Labels.Services;
using Elsa.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Labels.Endpoints.Labels;

[Area(AreaNames.Elsa)]
[ApiEndpoint(ControllerNames.Labels, "Update")]
[ProducesResponseType(typeof(Label), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public class Update : Controller
{
    private readonly ILabelStore _store;
    private readonly WorkflowSerializerOptionsProvider _workflowSerializerOptionsProvider;

    // ReSharper disable once ClassNeverInstantiated.Local
    private record LabelModel(string Name, string? Description, string? Color);

    public Update(ILabelStore store, WorkflowSerializerOptionsProvider workflowSerializerOptionsProvider)
    {
        _store = store;
        _workflowSerializerOptionsProvider = workflowSerializerOptionsProvider;
    }

    [HttpPost]
    public async Task<IActionResult> HandleAsync(string id, CancellationToken cancellationToken)
    {
        var label = await _store.FindByIdAsync(id, cancellationToken);

        if (label == null)
            return NotFound();
        
        var serializerOptions = _workflowSerializerOptionsProvider.CreateApiOptions();
        var (name, description, color) = (await Request.ReadFromJsonAsync<LabelModel>(serializerOptions, cancellationToken))!;

        label.Name = name.Trim();
        label.Description = description?.Trim();
        label.Color = color?.Trim();

        await _store.SaveAsync(label, cancellationToken);
        return Json(label, serializerOptions);
    }
}