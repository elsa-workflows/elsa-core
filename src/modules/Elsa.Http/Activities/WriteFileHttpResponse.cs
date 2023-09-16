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
using Microsoft.AspNetCore.StaticFiles;

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

        await WriteResponseAsync(context, httpContext.Response);
    }

    private async Task WriteResponseAsync(ActivityExecutionContext context, HttpResponse response)
    {
        // Set status code.
        var statusCode = HttpStatusCode.OK;
        response.StatusCode = (int)statusCode;

        // Get content and content type.
        var content = context.Get(Content);

        if (content == null)
            return;

        // Write content.
        var downloadables = await GetDownloadablesAsync(context, content);
        await SendDownloadablesAsync(context, response, downloadables);

        // Complete activity.
        await context.CompleteActivityAsync();
    }

    private async Task SendDownloadablesAsync(ActivityExecutionContext context, HttpResponse response, IEnumerable<Downloadable> downloadables)
    {
        var downloadableList = downloadables.ToList();

        switch (downloadableList.Count)
        {
            case 0:
                return;
            case 1:
            {
                var downloadable = downloadableList[0];
                await SendSingleFileAsync(context, response, downloadable);
                return;
            }
            default:
                await SendMultipleFilesAsync(context, response, downloadableList);
                break;
        }
    }

    private async Task SendSingleFileAsync(ActivityExecutionContext context, HttpResponse response, Downloadable downloadable)
    {
        var contentType = ContentType.GetOrDefault(context);
        var filename = FileName.GetOrDefault(context);
        filename = !string.IsNullOrWhiteSpace(filename) ? filename : !string.IsNullOrWhiteSpace(downloadable.Filename) ? downloadable.Filename : "file.bin";
        contentType = !string.IsNullOrWhiteSpace(contentType) ? contentType : !string.IsNullOrWhiteSpace(downloadable.ContentType) ? downloadable.ContentType : GetContentType(context, filename);
        response.ContentType = contentType;
        response.Headers.Add("Content-Disposition", $"attachment; filename=\"{filename}\"");
        await downloadable.Stream.CopyToAsync(response.Body);
    }

    private async Task SendMultipleFilesAsync(ActivityExecutionContext context, HttpResponse response, ICollection<Downloadable> downloadables)
    {
        var memoryStream = new MemoryStream();
        var currentFileIndex = 0;

        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            foreach (var downloadable in downloadables)
            {
                var entryName = !string.IsNullOrWhiteSpace(downloadable.Filename) ? downloadable.Filename : $"file-{currentFileIndex}.bin";
                var entry = archive.CreateEntry(entryName);
                var fileStream = downloadable.Stream;
                await using var entryStream = entry.Open();
                await fileStream.CopyToAsync(entryStream);
            }
        }

        var contentType = ContentType.GetOrDefault(context);
        var filename = FileName.GetOrDefault(context);

        contentType = !string.IsNullOrWhiteSpace(contentType) ? contentType : System.Net.Mime.MediaTypeNames.Application.Zip;
        filename = !string.IsNullOrWhiteSpace(filename) ? filename : "download.zip";

        memoryStream.Position = 0;
        response.ContentType = contentType;
        response.Headers.Add("Content-Disposition", $"attachment; filename=\"{filename}\"");
        await memoryStream.CopyToAsync(response.Body);
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
        var provider = context.GetService<IContentTypeProvider>() ?? new FileExtensionContentTypeProvider();
        return provider.TryGetContentType(filename, out var contentType) ? contentType : "application/octet-stream";
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

        await WriteResponseAsync(context, httpContext.Response);
    }
}