using System;
using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Samples.Server.Host.Endpoints.BrandModels;

/// <summary>
/// A sample API endpoint that returns a list of car models for a given brand. 
/// </summary>
[ApiController]
[Route("api/samples/brands/{brand}/models")]
[Produces(MediaTypeNames.Application.Json)]
public class List : Controller
{   
    [HttpGet]
    public IActionResult Handle(string brand)
    {
        var models = brand switch
        {
            "BMW" => new[] { "1 Series", "2 Series", "i3", "i4" },
            "Peugeot" => new[] { "208", "301", "508", "2008" },
            "Tesla" => new[] { "Roadster", "Model S", "Model 3", "Model X", "Model Y", "Cybertruck" },
            _ => Array.Empty<string>()
        };

        return Json(models);
    }
}