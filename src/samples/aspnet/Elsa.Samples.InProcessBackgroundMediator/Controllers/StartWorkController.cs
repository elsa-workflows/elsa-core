using Elsa.Samples.InProcessBackgroundMediator.Models;
using Elsa.Samples.InProcessBackgroundMediator.Notifications;
using Elsa.Samples.InProcessBackgroundMediator.Services;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Samples.InProcessBackgroundMediator.Controllers;

[ApiController]
[Route("work/start")]
public class StartWorkController : Controller
{
    private readonly IBackgroundEventPublisher _backgroundEventPublisher;

    public StartWorkController(IBackgroundEventPublisher backgroundEventPublisher)
    {
        _backgroundEventPublisher = backgroundEventPublisher;
    }
    
    public async Task<IActionResult> Handle(WorkDescription request)
    {
        // The background publisher will send the notification to a channel that is read from a background service.
        // This effectively allows you to enqueue work to be handled in the background.
        // IMPORTANT: this is only suitable for non-critical tasks, because if the application stops while there's unprocessed work in the channel, that work will get lost.  
        await _backgroundEventPublisher.PublishAsync(new SampleNotification(request.Message));
        
        // Not waiting for the work to be completed, we can return a response immediately.
        return Accepted();
    }
}