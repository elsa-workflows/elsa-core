using Elsa.AspNetCore.Attributes;
using Elsa.Labels.Entities;
using Elsa.Labels.Services;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Core.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Labels.Endpoints.Labels;

[Area(AreaNames.Elsa)]
[ApiEndpoint(ControllerNames.Labels, "Create")]
[ProducesResponseType(typeof(Label), StatusCodes.Status201Created)]
public class Create : Controller
{
    private readonly ILabelStore _store;
    private readonly IIdentityGenerator _identityGenerator;
    private readonly SerializerOptionsProvider _serializerOptionsProvider;

    // ReSharper disable once ClassNeverInstantiated.Local
    private record LabelModel(string Name, string? Description, string? Color);

    public Create(ILabelStore store, IIdentityGenerator identityGenerator, SerializerOptionsProvider serializerOptionsProvider)
    {
        _store = store;
        _identityGenerator = identityGenerator;
        _serializerOptionsProvider = serializerOptionsProvider;
    }

    [HttpPost]
    public async Task<IActionResult> HandleAsync(CancellationToken cancellationToken)
    {
        var serializerOptions = _serializerOptionsProvider.CreateApiOptions();
        var model = (await Request.ReadFromJsonAsync<LabelModel>(serializerOptions, cancellationToken))!;

        var label = new Label
        {
            Id = _identityGenerator.GenerateId(),
            Name = model.Name.Trim(),
            Description = model.Description?.Trim(),
            Color = model.Color?.Trim()
        };

        await _store.SaveAsync(label, cancellationToken);
        
        return CreatedAtRoute("Elsa.Labels.Get", new { Id = label.Id }, label);
    }
}