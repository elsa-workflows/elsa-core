using Elsa.Abstractions;
using Elsa.SasTokens.Contracts;
using Elsa.Workflows.Runtime;
using FastEndpoints;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
namespace Elsa.Workflows.Api.Endpoints.Bookmarks.Resume;
/// <summary>
/// Resumes a bookmarked workflow instance with the bookmark ID specified in the provided SAS token.
/// </summary>
[PublicAPI]
internal class Resume(ITokenService tokenService, IWorkflowResumer workflowResumer, IBookmarkQueue bookmarkQueue, IPayloadSerializer serializer) : ElsaEndpoint<Request>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Routes("/bookmarks/resume");
        Verbs(Http.GET, Http.POST);
        AllowAnonymous();
    }
    /// <inheritdoc />
    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var token = Query<string>("t")!;
        var asynchronous = Query<bool>("async", false);

        if (!tokenService.TryDecryptToken<BookmarkTokenPayload>(token, out var payload)) 
            AddError("Invalid token.");
        var input = HttpContext.Request.Method == HttpMethods.Post ? request.Input : GetInputFromQueryString();
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
            await Send.OkAsync(cancellationToken);
    }
    
    private IDictionary<string, object>? GetInputFromQueryString()
    {
        var inputJson = Query<string?>("in", false);
        if (string.IsNullOrWhiteSpace(inputJson))
            return null;
        try
        {
            return serializer.Deserialize<IDictionary<string, object>>(inputJson);
        }
        catch
        {
            AddError("Invalid input format. Expected a valid JSON string.");
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