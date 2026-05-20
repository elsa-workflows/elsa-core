using System.Text.Json;
using Elsa.Abstractions;
using Elsa.SasTokens.Contracts;
using Elsa.Workflows;
using Elsa.Workflows.Runtime;
using FastEndpoints;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace Elsa.Workflows.Api.Endpoints.Bookmarks.Resume;

/// <summary>
/// Resumes a bookmarked workflow instance with the bookmark ID specified in the provided SAS token.
/// </summary>
[PublicAPI]
internal class Resume(ITokenService tokenService, IWorkflowResumer workflowResumer, IBookmarkQueue bookmarkQueue, IPayloadSerializer payloadSerializer, IApiSerializer apiSerializer) : ElsaEndpointWithoutRequest
{
    /// <inheritdoc />
    public override void Configure()
    {
        Routes("/bookmarks/resume");
        Verbs(Http.GET, Http.POST);
        AllowAnonymous();
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var token = Query<string?>("t", false);

        if (string.IsNullOrWhiteSpace(token) || !tokenService.TryDecryptToken<BookmarkTokenPayload>(token, out var payload))
        {
            AddError("Invalid token.");
            await Send.ErrorsAsync(cancellation: cancellationToken);
            return;
        }

        var asynchronous = Query<bool>("async", false);
        var input = await GetInputAsync(cancellationToken);

        if (ValidationFailed)
        {
            await Send.ErrorsAsync(cancellation: cancellationToken);
            return;
        }
        
        // Some clients, like Blazor, may prematurely cancel their request upon navigation away from the page.
        // In this case, we don't want to cancel the workflow execution.
        // We need to better understand the conditions that cause this.
        var workflowCancellationToken = CancellationToken.None;
        await ResumeBookmarkedWorkflowAsync(payload, input, asynchronous, workflowCancellationToken);
        
        if (!HttpContext.Response.HasStarted)
            await Send.OkAsync(cancellation: cancellationToken);
    }
    
    private IDictionary<string, object>? GetInputFromQueryString()
    {
        var inputJson = Query<string?>("in", false);
        if (string.IsNullOrWhiteSpace(inputJson))
            return null;

        try
        {
            return payloadSerializer.Deserialize<IDictionary<string, object>>(inputJson);
        }
        catch (Exception e) when (e is JsonException or NotSupportedException or InvalidOperationException or FormatException or ArgumentException)
        {
            AddError("Invalid input format. Expected a valid JSON string.");
            return null;
        }
    }

    private async ValueTask<IDictionary<string, object>?> GetInputAsync(CancellationToken cancellationToken)
    {
        return HttpContext.Request.Method == HttpMethods.Post
            ? await GetInputFromBodyAsync(cancellationToken)
            : GetInputFromQueryString();
    }

    private async ValueTask<IDictionary<string, object>?> GetInputFromBodyAsync(CancellationToken cancellationToken)
    {
        if (HttpContext.Request.ContentLength == 0)
            return null;

        using var reader = new StreamReader(HttpContext.Request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(body))
            return null;

        try
        {
            var request = apiSerializer.Deserialize<Request>(body);
            if (request == null)
            {
                AddError("Invalid input format. Expected a valid JSON request body.");
                return null;
            }

            return request.Input;
        }
        catch (Exception e) when (e is JsonException or NotSupportedException or InvalidOperationException or FormatException or ArgumentException)
        {
            AddError("Invalid input format. Expected a valid JSON request body.");
            return null;
        }
    }
    
    private async Task ResumeBookmarkedWorkflowAsync(BookmarkTokenPayload tokenPayload, IDictionary<string, object>? input, bool asynchronous, CancellationToken cancellationToken)
    {
        var bookmarkId = tokenPayload.BookmarkId;
        var workflowInstanceId = tokenPayload.WorkflowInstanceId;

        if (asynchronous)
        {
            var item = new NewBookmarkQueueItem
            {
                BookmarkId = bookmarkId,
                WorkflowInstanceId = workflowInstanceId,
                Options = new()
                {
                    Input = input
                }
            };

            await bookmarkQueue.EnqueueAsync(item, cancellationToken);
            return;
        }

        var resumeRequest = new ResumeBookmarkRequest
        {
            BookmarkId = bookmarkId,
            WorkflowInstanceId = workflowInstanceId,
            Input = input
        };
        
        await workflowResumer.ResumeAsync(resumeRequest, cancellationToken);
    }
}

/// <summary>
/// The request model for the Resume endpoint.
/// </summary>
internal class Request
{
    /// <summary>
    /// The input to provide to the workflow when resuming.
    /// </summary>
    public IDictionary<string, object>? Input { get; set; }
}
