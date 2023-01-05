using Elsa.Abstractions;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.Workflows.Api.Endpoints.Events.Trigger;

/// <summary>
/// Triggers all workflows that are waiting for the specified event.
/// </summary>
public class Trigger : ElsaEndpoint<Request, Response>
{
    private readonly IEventPublisher _eventPublisher;

    /// <inheritdoc />
    public Trigger(IEventPublisher eventPublisher)
    {
        _eventPublisher = eventPublisher;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/events/{eventName}/trigger");
        ConfigurePermissions("trigger:event");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        await _eventPublisher.DispatchAsync(request.EventName, request.CorrelationId, cancellationToken: cancellationToken);
        
        if (!HttpContext.Response.HasStarted)
        {
            await SendOkAsync(cancellationToken);
        }
    }
}
