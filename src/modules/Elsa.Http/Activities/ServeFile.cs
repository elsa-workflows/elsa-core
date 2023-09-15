using System.Collections;
using System.Net;
using Elsa.Extensions;
using Elsa.Http.ContentWriters;
using Elsa.Http.Contracts;
using Elsa.Http.Models;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Exceptions;
using Elsa.Workflows.Core.Models;
using Microsoft.AspNetCore.Http;

namespace Elsa.Http;

/// <summary>
/// Sends a file to the HTTP response.
/// </summary>
[Activity("Elsa", "HTTP", "Send one ore more files (zipped) to the HTTP response.", DisplayName = "HTTP Response")]
public class ServeFile : Activity
{
    /// <summary>
    /// The MIME type of the file to serve.
    /// </summary>
    [Input(Description = "The MIME type of the file to serve. Leave empty to let the system determine the MIME type.")]
    public Input<string?> MimeType { get; set; } = default!;

    /// <summary>
    /// The name of the file to serve.
    /// </summary>
    [Input(Description = "The name of the file to serve. Leave empty to let the system determine the file name.")]
    public Input<string?> FileName { get; set; } = default!;
    
    /// <summary>
    /// The file content to serve. Supports byte array, streams, string, Uri and an array of the aforementioned types.
    /// </summary>
    [Input(Description = "The file content to serve. Supports byte array, streams, string, Uri and an array of the aforementioned types.")]
    public Input<object> Content { get; set; } = default!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var httpContextAccessor = context.GetRequiredService<IHttpContextAccessor>();
        var httpContext = httpContextAccessor.HttpContext;
        
        if (httpContext == null)
        {
            // We're executing in a non-HTTP context (e.g. in a virtual actor).
            // Create a bookmark to allow the invoker to export the state and resume execution from there.

            context.CreateBookmark(OnResumeAsync);
            return;
        }

        await WriteResponseAsync(context, httpContext.Response);
    }
    
    private async Task WriteResponseAsync(ActivityExecutionContext context, HttpResponse response)
    {
        // Set status code.
        var statusCode = HttpStatusCode.OK;
        response.StatusCode = (int)statusCode;

        // Add headers.

        // Get content and content type.
        var content = context.Get(Content);

        if (content == null)
            return;

        var mimeType = MimeType.GetOrDefault(context);

        if (string.IsNullOrWhiteSpace(mimeType))
            mimeType = System.Net.Mime.MediaTypeNames.Application.Octet;
        
        // Set content type.
        response.ContentType = mimeType;

        // Write content.
        var downloadables = await GetDownloadablesAsync(context, content);
        await SendDownloadablesAsync(context, response, downloadables);
        
        // Complete activity.
        await context.CompleteActivityAsync();
    }

    private async Task SendDownloadablesAsync(ActivityExecutionContext context, HttpResponse response, IEnumerable<Downloadable> downloadables)
    {
        var downloadableList = downloadables.ToList();
        
        if(downloadableList.Count == 0)
            return;

        if (downloadableList.Count == 1)
        {
            // TODO: Send the stream directly to the response.
            return;
        }
        
        // TODO: Generate a zip file and send it to the response.
    }

    private async Task<IEnumerable<Downloadable>> GetDownloadablesAsync(ActivityExecutionContext context, object content)
    {
        var manager = context.GetRequiredService<IDownloadableManager>();
        return await manager.GetDownloadablesAsync(content, context.CancellationToken);
    }
    
    private async ValueTask OnResumeAsync(ActivityExecutionContext context)
    {
        var httpContextAccessor = context.GetRequiredService<IHttpContextAccessor>();
        var httpContext = httpContextAccessor.HttpContext;

        if (httpContext == null)
        {
            // We're not in an HTTP context, so let's fail.
            throw new Exception("Cannot execute in a non-HTTP context");
        }

        await WriteResponseAsync(context, httpContext.Response);
    }
}