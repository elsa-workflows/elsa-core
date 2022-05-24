using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Workflows.Api.Models;
using Elsa.AspNetCore;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Services;
using Elsa.Serialization.Converters;
using Elsa.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Workflows.Api.Endpoints.ActivityDescriptors;

[Area(AreaNames.Elsa)]
[ApiEndpoint(ControllerNames.ActivityDescriptors, "List")]
[ProducesResponseType(typeof(ActivityDescriptors), StatusCodes.Status200OK, "application/json")]
public class List : Controller
{
    private readonly IActivityRegistry _registry;
    private readonly IWellKnownTypeRegistry _wellKnownTypeRegistry;

    public List(IActivityRegistry registry, IWellKnownTypeRegistry wellKnownTypeRegistry)
    {
        _registry = registry;
        _wellKnownTypeRegistry = wellKnownTypeRegistry;
    }

    [HttpGet]
    public IActionResult Handle()
    {
        var descriptors = _registry.ListAll().ToList();

        var model = new
        {
            ActivityDescriptors = descriptors
        };

        var serializerOptions = new JsonSerializerOptions
        {
            Converters =
            {
                new JsonStringEnumConverter(),
                new TypeJsonConverter(_wellKnownTypeRegistry)
            },
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
        };

        return Json(model, serializerOptions);
    }
}

public record ActivityDescriptors(ICollection<ActivityDescriptor> Items) : ListModel<ActivityDescriptor>(Items);