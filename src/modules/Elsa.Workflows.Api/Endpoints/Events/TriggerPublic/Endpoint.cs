using Elsa.Abstractions;
using Elsa.SasTokens.Contracts;
using Elsa.Workflows.Runtime;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.Events.TriggerPublic;

/// <summary>
/// Resumes a workflow instance blocked by a specified event encoded in the provided SAS token.
/// </summary>
[PublicAPI]
internal class Trigger : ElsaEndpointWithoutRequest
{
    private readonly ITokenService _tokenService;
    private readonly IEventPublisher _eventPublisher;

    /// <inheritdoc />
    public Trigger(ITokenService tokenService, IEventPublisher eventPublisher)
    {
        _tokenService = tokenService;
        _eventPublisher = eventPublisher;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/events/trigger");
        AllowAnonymous();
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var token = Query<string>("t")!;

        if (!_tokenService.TryDecryptToken<EventTokenPayload>(token, out var payload))
        {
            AddError("Invalid token.");
            await SendErrorsAsync(cancellation: cancellationToken);
            return;
        }

        var eventName = payload.EventName;
        var workflowInstanceId = payload.WorkflowInstanceId;
        await _eventPublisher.PublishAsync(eventName, workflowInstanceId: workflowInstanceId, cancellationToken: cancellationToken);
        
        if (!HttpContext.Response.HasStarted)
            await SendOkAsync(cancellationToken);
    }
}