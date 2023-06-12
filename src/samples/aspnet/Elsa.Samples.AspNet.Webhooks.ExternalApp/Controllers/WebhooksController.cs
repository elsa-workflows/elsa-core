using Elsa.Samples.AspNet.Webhooks.ExternalApp.Jobs;
using Elsa.Samples.AspNet.Webhooks.ExternalApp.Models;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Samples.AspNet.Webhooks.ExternalApp.Controllers;

[ApiController]
[Route("webhooks")]
public class WebhooksController : ControllerBase
{
    private readonly DeliverFoodJob _deliverFoodJob;

    public WebhooksController(DeliverFoodJob deliverFoodJob)
    {
        _deliverFoodJob = deliverFoodJob;
    }
    
    [HttpPost("run-task")]
    public IActionResult RunTask(WebhookEvent<RunTaskPayload> model)
    {
        Task.Factory.StartNew(() => _deliverFoodJob.RunAsync(model.Payload));
        return Accepted();
    }
}