using System.IO.Compression;
using System.Net;
using Elsa.Extensions;
using Elsa.Http.Contracts;
using Elsa.Http.Models;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Exceptions;
using Elsa.Workflows.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;

namespace Elsa.Http;

/// <summary>
/// Sends a file to the HTTP response.
/// </summary>
[Activity("Elsa", "HTTP", "Send one ore more files (zipped) to the HTTP response.", DisplayName = "HTTP File Response")]
public class WriteFileHttpResponse : Activity
{
    /// <summary>
    /// The MIME type of the file to serve.
    /// </summary>
    [Input(Description = "The content type of the file to serve. Leave empty to let the system determine the content type.")]
    public Input<string?> ContentType { get; set; } = default!;

    /// <summary>
    /// The name of the file to serve.
    /// </summary>
    [Input(Description = "The name of the file to serve. Leave empty to let the system determine the file name.")]
    public Input<string?> FileName { get; set; } = default!;

    /// <summary>
    /// The file content to serve. Supports byte array, streams, string, Uri and an array of the aforementioned types.
    /// </summary>
    [Input(Description = "The file content to serve. Supports various types, such as byte array, stream, string, Uri, Downloadable and a (mixed) array of the aforementioned types.")]
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

            context.CreateBookmark(OnResumeAsync, BookmarkMetadata.HttpCrossBoundary);
            return;
        }

        await WriteResponseAsync(context, httpContext);
    }

    private async Task WriteResponseAsync(ActivityExecutionContext context, HttpContext httpContext)
    {
        // Set status code.
        var statusCode = HttpStatusCode.OK;
        var response = httpContext.Response;
        response.StatusCode = (int)statusCode;

        // Get content and content type.
        var content = context.Get(Content);

        if (content == null)
            return;

        // Write content.
        var downloadables = await GetDownloadablesAsync(context, content);
        await SendDownloadablesAsync(context, httpContext, downloadables);

        // Complete activity.
        await context.CompleteActivityAsync();
    }

    private async Task SendDownloadablesAsync(ActivityExecutionContext context, HttpContext httpContext, IEnumerable<Downloadable> downloadables)
    {
        var downloadableList = downloadables.ToList();

        switch (downloadableList.Count)
        {
            case 0:
                return;
            case 1:
            {
                var downloadable = downloadableList[0];
                await SendSingleFileAsync(context, httpContext, downloadable);
                return;
            }
            default:
                await SendMultipleFilesAsync(context, httpContext, downloadableList);
                break;
        }
    }

    private async Task SendSingleFileAsync(ActivityExecutionContext context, HttpContext httpContext, Downloadable downloadable)
    {
        var contentType = ContentType.GetOrDefault(context);
        var filename = FileName.GetOrDefault(context);
        filename = !string.IsNullOrWhiteSpace(filename) ? filename : !string.IsNullOrWhiteSpace(downloadable.Filename) ? downloadable.Filename : "file.bin";
        contentType = !string.IsNullOrWhiteSpace(contentType) ? contentType : !string.IsNullOrWhiteSpace(downloadable.ContentType) ? downloadable.ContentType : GetContentType(context, filename);
        await SendFileStream(httpContext, downloadable.Stream, contentType, filename);
    }

    private async Task SendMultipleFilesAsync(ActivityExecutionContext context, HttpContext httpContext, ICollection<Downloadable> downloadables)
    {
        var logger = context.GetRequiredService<ILogger<WriteFileHttpResponse>>();
        
        // 1. Create a temporary file
        var tempFilePath = Path.GetTempFileName();
        var currentFileIndex = 0;

        // 2. Write the zip archive to the temporary file
        await using var tempFileStream = new FileStream(tempFilePath, FileMode.Create);
        using var zipArchive = new ZipArchive(tempFileStream, ZipArchiveMode.Create, true);

        foreach (var downloadable in downloadables)
        {
            var entryName = !string.IsNullOrWhiteSpace(downloadable.Filename) ? downloadable.Filename : $"file-{currentFileIndex}.bin";
            var entry = zipArchive.CreateEntry(entryName);
            var fileStream = downloadable.Stream;
            await using var entryStream = entry.Open();
            await fileStream.CopyToAsync(entryStream);
            await entryStream.FlushAsync();
            entryStream.Close();
            currentFileIndex++;
        }
        
        var contentType = ContentType.GetOrDefault(context);
        var filename = FileName.GetOrDefault(context);

        contentType = !string.IsNullOrWhiteSpace(contentType) ? contentType : System.Net.Mime.MediaTypeNames.Application.Zip;
        filename = !string.IsNullOrWhiteSpace(filename) ? filename : "download.zip";
        
        // 3. Use FileStreamResult to stream the temporary file back to the client
        var resultStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
        await SendFileStream(httpContext, resultStream, contentType, filename);
        
        // 4. Delete the temporary file.
        try
        {
            File.Delete(tempFilePath);
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Failed to delete temporary file {TempFilePath}", tempFilePath);
        }
    }

    private async Task SendFileStream(HttpContext httpContext, Stream source, string contentType, string filename)
    {
        source.Seek(0, SeekOrigin.Begin);
        
        var result = new FileStreamResult(source, contentType)
        {
            EnableRangeProcessing = true,
            FileDownloadName = filename
        };
        
        var actionContext = new ActionContext(httpContext, httpContext.GetRouteData(), new ActionDescriptor());
        await result.ExecuteResultAsync(actionContext);
    }

    /// <summary>
    /// Leverages the <see cref="IDownloadableManager"/> to get a list of <see cref="Downloadable"/> instances from the <paramref name="content"/>.
    /// </summary>
    private async Task<IEnumerable<Downloadable>> GetDownloadablesAsync(ActivityExecutionContext context, object content)
    {
        var manager = context.GetRequiredService<IDownloadableManager>();
        return await manager.GetDownloadablesAsync(content, context.CancellationToken);
    }

    private string GetContentType(ActivityExecutionContext context, string filename)
    {
        var provider = context.GetRequiredService<IContentTypeProvider>();
        return provider.TryGetContentType(filename, out var contentType) ? contentType : System.Net.Mime.MediaTypeNames.Application.Octet;
    }

    private static string CreateContentDisposition(string filename)
    {
        var contentDisposition = new System.Net.Mime.ContentDisposition
        {
            FileName = filename
        };

        return contentDisposition.ToString();
    }

    private async ValueTask OnResumeAsync(ActivityExecutionContext context)
    {
        var httpContextAccessor = context.GetRequiredService<IHttpContextAccessor>();
        var httpContext = httpContextAccessor.HttpContext;

        if (httpContext == null)
        {
            // We're not in an HTTP context, so let's fail.
            throw new FaultException("Cannot execute in a non-HTTP context");
        }

        await WriteResponseAsync(context, httpContext);
    }
}