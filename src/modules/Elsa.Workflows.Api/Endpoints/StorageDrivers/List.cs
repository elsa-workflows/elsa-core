using System.Collections.Generic;
using System.Linq;
using Elsa.AspNetCore.Attributes;
using Elsa.AspNetCore.Models;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Core.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
// ReSharper disable NotAccessedPositionalProperty.Global

namespace Elsa.Workflows.Api.Endpoints.StorageDrivers;

[Area(AreaNames.Elsa)]
[ApiEndpoint(ControllerNames.StorageDrivers, "List")]
[ProducesResponseType(typeof(StorageDrivers), StatusCodes.Status200OK, "application/json")]
public class List : Controller
{
    private readonly IStorageDriverManager _registry;
    private readonly WorkflowSerializerOptionsProvider _serializerOptionsProvider;

    public List(IStorageDriverManager registry, WorkflowSerializerOptionsProvider serializerOptionsProvider)
    {
        _registry = registry;
        _serializerOptionsProvider = serializerOptionsProvider;
    }

    [HttpGet]
    public IActionResult Handle()
    {
        var drivers = _registry.List();
        var descriptors = drivers.Select(x => new StorageDriverDescriptor(x.Id, x.DisplayName)).ToList();
        var model = new StorageDrivers(descriptors);
        var serializerOptions = _serializerOptionsProvider.CreateApiOptions();

        return Json(model, serializerOptions);
    }
}

public record StorageDrivers(ICollection<StorageDriverDescriptor> Items) : ListModel<StorageDriverDescriptor>(Items);

public record StorageDriverDescriptor(string Id, string DisplayName);