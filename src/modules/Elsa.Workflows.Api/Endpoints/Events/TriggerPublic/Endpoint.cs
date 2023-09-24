using Elsa.Abstractions;
using Elsa.Http.Contracts;
using Elsa.Http.Models;
using Elsa.SasTokens.Contracts;
using Elsa.Workflows.Runtime.Contracts;
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
    private readonly IHttpBookmarkProcessor _httpBookmarkProcessor;

    /// <inheritdoc />
    public Trigger(ITokenService tokenService, IEventPublisher eventPublisher, IHttpBookmarkProcessor httpBookmarkProcessor)
    {
        _tokenService = tokenService;
        _eventPublisher = eventPublisher;
        _httpBookmarkProcessor = httpBookmarkProcessor;
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
        var results = await _eventPublisher.PublishAsync(eventName, workflowInstanceId: workflowInstanceId, cancellationToken: cancellationToken);
        
        await _httpBookmarkProcessor.ProcessBookmarks(results, cancellationToken: cancellationToken);

        if (!HttpContext.Response.HasStarted)
            await SendOkAsync(cancellationToken);
    }
}