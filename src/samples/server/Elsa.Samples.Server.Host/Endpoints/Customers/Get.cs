using System;
using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Samples.Server.Host.Endpoints.Customers;

/// <summary>
/// A sample API endpoint that returns a list of car models for a given brand. 
/// </summary>
[ApiController]
[Route("api/samples/customer/{id}")]
[Produces(MediaTypeNames.Application.Json)]
public class Get : Controller
{   
    [HttpGet]
    public IActionResult Handle(Guid id)
    {
        var model = new
        {
            Id = id,
            Name = "Customer name"
        };
        
        return Json(model);
    }
}