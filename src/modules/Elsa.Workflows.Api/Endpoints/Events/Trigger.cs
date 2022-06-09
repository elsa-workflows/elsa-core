using Elsa.AspNetCore.Attributes;
using Elsa.Workflows.Api.ApiResults;
using Elsa.Workflows.Api.Models;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Runtime.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Workflows.Api.Endpoints.Events;

[Area(AreaNames.Elsa)]
[ApiEndpoint(ControllerNames.Events, "Trigger")]
[ProducesResponseType(typeof(ProcessStimulusResults), StatusCodes.Status200OK, "application/json")]
public class Trigger : Controller
{
    private readonly IHasher _hasher;

    public Trigger(IHasher hasher)
    {
        _hasher = hasher;
    }

    [HttpPost]
    public IActionResult Handle(string eventName)
    {
        var eventBookmark = new EventBookmarkData(eventName);
        var hash = _hasher.Hash(eventBookmark);
        var stimulus = Stimulus.Standard(ActivityTypeNameHelper.GenerateTypeName<Event>(), hash);
        return new ProcessStimulusResult(stimulus);
    }
}