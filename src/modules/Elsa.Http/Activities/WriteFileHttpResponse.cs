using Elsa.Extensions;
using Elsa.Http.Contracts;
using Elsa.Http.Models;
using Elsa.Http.Options;
using Elsa.Http.Services;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Exceptions;
using Elsa.Workflows.Core.Models;
using FluentStorage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using EntityTagHeaderValue = System.Net.Http.Headers.EntityTagHeaderValue;
using RangeHeaderValue = System.Net.Http.Headers.RangeHeaderValue;

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
    /// The Entity Tag of the file to serve.
    /// </summary>
    [Input(Description = "The Entity Tag of the file to serve. Leave empty to let the system determine the Entity Tag.")]
    public Input<string?> EntityTag { get; set; } = default!;

    /// <summary>
    /// The file content to serve. Supports byte array, streams, string, Uri and an array of the aforementioned types.
    /// </summary>
    [Input(Description = "The file content to serve. Supports various types, such as byte array, stream, string, Uri, Downloadable and a (mixed) array of the aforementioned types.")]
    public Input<object> Content { get; set; } = default!;

    /// <summary>
    /// Whether to enable resumable downloads. When enabled, the client can resume a download if the connection is lost.
    /// </summary>
    [Input(Description = "Whether to enable resumable downloads. When enabled, the client can resume a download if the connection is lost.")]
    public Input<bool> EnableResumableDownloads { get; set; } = default!;

    /// <summary>
    /// The correlation ID of the download. Used to resume a download.
    /// </summary>
    [Input(Description = "The correlation ID of the download used to resume a download. If left empty, the x-elsa-download-id header will be used.")]
    public Input<string> DownloadCorrelationId { get; set; } = default!;

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
        // Get content and content type.
        var content = context.Get(Content);

        if (content == null)
            return;

        // Write content.
        var downloadables = await GetDownloadablesAsync(context, httpContext, content);
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
                SendNoContent(context, httpContext);
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

    private void SendNoContent(ActivityExecutionContext context, HttpContext httpContext)
    {
        httpContext.Response.StatusCode = StatusCodes.Status204NoContent;
    }

    private async Task SendSingleFileAsync(ActivityExecutionContext context, HttpContext httpContext, Downloadable downloadable)
    {
        var contentType = ContentType.GetOrDefault(context);
        var filename = FileName.GetOrDefault(context);
        var eTag = EntityTag.GetOrDefault(context);
        filename = !string.IsNullOrWhiteSpace(filename) ? filename : !string.IsNullOrWhiteSpace(downloadable.Filename) ? downloadable.Filename : "file.bin";
        contentType = !string.IsNullOrWhiteSpace(contentType) ? contentType : !string.IsNullOrWhiteSpace(downloadable.ContentType) ? downloadable.ContentType : GetContentType(context, filename);
        eTag = !string.IsNullOrWhiteSpace(eTag) ? eTag : !string.IsNullOrWhiteSpace(downloadable.ETag) ? downloadable.ETag : default;

        var eTagHeaderValue = !string.IsNullOrWhiteSpace(eTag) ? new EntityTagHeaderValue(eTag) : default;

        await SendFileStream(context, httpContext, downloadable.Stream, contentType, filename, eTagHeaderValue);
    }

    private async Task SendMultipleFilesAsync(ActivityExecutionContext context, HttpContext httpContext, ICollection<Downloadable> downloadables)
    {
        // If resumable downloads are enabled, check to see if we have a cached file.
        var (zipBlob, zipStream, cleanupCallback) = await TryLoadCachedFileAsync(context) ?? await GenerateZipFileAsync(context, httpContext, downloadables);

        try
        {
            // Send the zip stream the temporary file back to the client.
            var contentType = zipBlob.Metadata["ContentType"];
            var downloadAsFilename = zipBlob.Metadata["Filename"];
            var eTag = $"\"{zipBlob.LastModificationTime?.ToString("O")}\"";
            var eTagHeaderValue = new EntityTagHeaderValue(eTag);
            await SendFileStream(context, httpContext, zipStream, contentType, downloadAsFilename, eTagHeaderValue);

            // TODO: Delete the cached file after the workflow completes.
        }
        catch (Exception e)
        {
            var logger = context.GetRequiredService<ILogger<WriteFileHttpResponse>>();
            logger.LogWarning(e, "Failed to send zip file to HTTP response");
        }
        finally
        {
            // Delete any temporary files.
            await cleanupCallback();
        }
    }

    private async Task<(Blob, Stream, Func<ValueTask>)> GenerateZipFileAsync(ActivityExecutionContext context, HttpContext httpContext, ICollection<Downloadable> downloadables)
    {
        var cancellationToken = context.CancellationToken;

        // Create a temporary file.
        var tempFilePath = Path.GetTempFileName();

        // Create a zip archive from the downloadables.
        var zipService = context.GetRequiredService<ZipService>();
        await zipService.CreateZipArchiveAsync(tempFilePath, downloadables, cancellationToken);

        // Create a blob with metadata for resuming the download.
        var contentType = ContentType.GetOrDefault(context);
        var downloadAsFilename = FileName.GetOrDefault(context);
        var zipBlob = zipService.CreateBlob(tempFilePath, downloadAsFilename, contentType);

        // If resumable downloads are enabled, cache the file.
        var enableResumableDownloads = EnableResumableDownloads.GetOrDefault(context, () => false);
        var downloadCorrelationId = DownloadCorrelationId.GetOrDefault(context);

        // If download correlation ID is not set, try and get it from the request headers.
        if (string.IsNullOrWhiteSpace(downloadCorrelationId))
            downloadCorrelationId = httpContext.Request.Headers["x-elsa-download-id"];

        if (enableResumableDownloads && !string.IsNullOrWhiteSpace(downloadCorrelationId))
            await zipService.CreateCachedZipBlobAsync(tempFilePath, downloadCorrelationId, downloadAsFilename, contentType, cancellationToken);

        var zipStream = File.OpenRead(tempFilePath);
        return (zipBlob, zipStream, Cleanup);

        ValueTask Cleanup()
        {
            var logger = context.GetRequiredService<ILogger<WriteFileHttpResponse>>();
            try
            {
                File.Delete(tempFilePath);
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "Failed to delete temporary file {TempFilePath}", tempFilePath);
            }

            return default;
        }
    }

    private async Task<(Blob, Stream, Func<ValueTask>)?> TryLoadCachedFileAsync(ActivityExecutionContext context)
    {
        var enableResumableDownloads = EnableResumableDownloads.GetOrDefault(context, () => false);
        var downloadCorrelationId = DownloadCorrelationId.GetOrDefault(context, () => context.WorkflowExecutionContext.CorrelationId ?? "");

        if (!enableResumableDownloads || string.IsNullOrWhiteSpace(downloadCorrelationId))
            return null;

        var cancellationToken = context.CancellationToken;
        var zipService = context.GetRequiredService<ZipService>();
        var tuple = await zipService.LoadCachedZipBlobAsync(downloadCorrelationId, cancellationToken);

        if (tuple == null)
            return null;

        return (tuple.Value.Item1, tuple.Value.Item2, Noop);

        ValueTask Noop() => default;
    }

    private async Task SendFileStream(ActivityExecutionContext context, HttpContext httpContext, Stream source, string contentType, string filename, EntityTagHeaderValue? eTag)
    {
        source.Seek(0, SeekOrigin.Begin);

        var result = new FileStreamResult(source, contentType)
        {
            EnableRangeProcessing = true,
            EntityTag = eTag != null ? new Microsoft.Net.Http.Headers.EntityTagHeaderValue(eTag.ToString()) : default,
            FileDownloadName = filename
        };

        var actionContext = new ActionContext(httpContext, httpContext.GetRouteData(), new ActionDescriptor());
        await result.ExecuteResultAsync(actionContext);
    }

    /// <summary>
    /// Leverages the <see cref="IDownloadableManager"/> to get a list of <see cref="Downloadable"/> instances from the <paramref name="content"/>.
    /// </summary>
    private async Task<IEnumerable<Downloadable>> GetDownloadablesAsync(ActivityExecutionContext context, HttpContext httpContext, object content)
    {
        var manager = context.GetRequiredService<IDownloadableManager>();
        var headers = httpContext.Request.Headers;
        var eTag = headers.TryGetValue(HeaderNames.IfMatch, out var header) ? new EntityTagHeaderValue(header.ToString()) : default;
        var range = headers.TryGetValue(HeaderNames.Range, out header) ? RangeHeaderValue.Parse(header.ToString()) : default;
        var options = new DownloadableOptions { ETag = eTag, Range = range };
        return await manager.GetDownloadablesAsync(content, options, context.CancellationToken);
    }

    private string GetContentType(ActivityExecutionContext context, string filename)
    {
        var provider = context.GetRequiredService<IContentTypeProvider>();
        return provider.TryGetContentType(filename, out var contentType) ? contentType : System.Net.Mime.MediaTypeNames.Application.Octet;
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