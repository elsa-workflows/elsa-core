using Elsa.Samples.Webhooks.ExternalApp.Models;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Samples.Webhooks.ExternalApp.Controllers;

[ApiController]
[Route("webhooks")]
public class WebhooksController : ControllerBase
{
    private readonly BackgroundWorker _backgroundWorker;

    public WebhooksController(BackgroundWorker backgroundWorker)
    {
        _backgroundWorker = backgroundWorker;
    }
    
    [HttpPost("run-task")]
    public IActionResult RunTask(WebhookEvent<RunTaskPayload> model)
    {
        Task.Factory.StartNew(() => _backgroundWorker.RunAsync(model.Payload));
        return Accepted();
    }
}