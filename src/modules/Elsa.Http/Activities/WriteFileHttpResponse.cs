using System.Security.Cryptography;
using Elsa.Extensions;
using Elsa.Http.Contracts;
using Elsa.Http.Exceptions;
using Elsa.Http.Models;
using Elsa.Http.Options;
using Elsa.Http.Services;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Exceptions;
using Elsa.Workflows.Core.Models;
using FluentStorage.Blobs;
using FluentStorage.Utils.Extensions;
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
    public Input<string?> Filename { get; set; } = default!;

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
    [Input(Description = "The correlation ID of the download used to resume a download. If left empty, the x-download-id header will be used.")]
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

        // Write content.
        var downloadables = GetDownloadables(context, httpContext, content).ToList();
        await SendDownloadablesAsync(context, httpContext, downloadables);

        // Complete activity.
        await context.CompleteActivityAsync();
    }

    private async Task SendDownloadablesAsync(ActivityExecutionContext context, HttpContext httpContext, IEnumerable<Func<ValueTask<Downloadable>>> downloadables)
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

    private async Task SendSingleFileAsync(ActivityExecutionContext context, HttpContext httpContext, Func<ValueTask<Downloadable>> downloadableFunc)
    {
        var contentType = ContentType.GetOrDefault(context);
        var filename = Filename.GetOrDefault(context);
        var eTag = EntityTag.GetOrDefault(context);
        var downloadable = await downloadableFunc();
        filename = !string.IsNullOrWhiteSpace(filename) ? filename : !string.IsNullOrWhiteSpace(downloadable.Filename) ? downloadable.Filename : "file.bin";
        contentType = !string.IsNullOrWhiteSpace(contentType) ? contentType : !string.IsNullOrWhiteSpace(downloadable.ContentType) ? downloadable.ContentType : GetContentType(context, filename);
        eTag = !string.IsNullOrWhiteSpace(eTag) ? eTag : !string.IsNullOrWhiteSpace(downloadable.ETag) ? downloadable.ETag : default;

        var eTagHeaderValue = !string.IsNullOrWhiteSpace(eTag) ? new EntityTagHeaderValue(eTag) : default;
        var stream = downloadable.Stream;
        await SendFileStream(context, httpContext, stream, contentType, filename, eTagHeaderValue);
    }

    private async Task SendMultipleFilesAsync(ActivityExecutionContext context, HttpContext httpContext, ICollection<Func<ValueTask<Downloadable>>> downloadables)
    {
        // If resumable downloads are enabled, check to see if we have a cached file.
        var (zipBlob, zipStream, cleanupCallback) = await TryLoadCachedFileAsync(context, httpContext) ?? await GenerateZipFileAsync(context, httpContext, downloadables);

        try
        {
            // Send the temporary file back to the client.
            var contentType = zipBlob.Metadata["ContentType"];
            var downloadAsFilename = zipBlob.Metadata["Filename"];
            var hash = ComputeHash(zipStream);
            var eTag = $"\"{hash}\"";
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

    private string ComputeHash(Stream stream)
    {
        stream.Seek(0, SeekOrigin.Begin);
        var bytes = stream.ToByteArray()!;
        using var md5Hash = MD5.Create();
        var hash = md5Hash.ComputeHash(bytes);
        stream.Seek(0, SeekOrigin.Begin);
        return Convert.ToBase64String(hash);
    }

    private async Task<(Blob, Stream, Func<ValueTask>)> GenerateZipFileAsync(ActivityExecutionContext context, HttpContext httpContext, ICollection<Func<ValueTask<Downloadable>>> downloadables)
    {
        var cancellationToken = context.CancellationToken;
        var downloadCorrelationId = GetDownloadCorrelationId(context, httpContext);
        var contentType = ContentType.GetOrDefault(context);
        var downloadAsFilename = Filename.GetOrDefault(context);
        var zipService = context.GetRequiredService<ZipManager>();
        var (zipBlob, zipStream, cleanup) = await zipService.CreateAsync(downloadables, true, downloadCorrelationId, downloadAsFilename, contentType, cancellationToken);

        return (zipBlob, zipStream, Cleanup);

        ValueTask Cleanup()
        {
            cleanup();
            return default;
        }
    }

    private async Task<(Blob, Stream, Func<ValueTask>)?> TryLoadCachedFileAsync(ActivityExecutionContext context, HttpContext httpContext)
    {
        var downloadCorrelationId = GetDownloadCorrelationId(context, httpContext);

        if (string.IsNullOrWhiteSpace(downloadCorrelationId))
            return null;

        var cancellationToken = context.CancellationToken;
        var zipService = context.GetRequiredService<ZipManager>();
        var tuple = await zipService.LoadAsync(downloadCorrelationId, cancellationToken);

        if (tuple == null)
            return null;

        return (tuple.Value.Item1, tuple.Value.Item2, Noop);

        ValueTask Noop() => default;
    }

    private string GetDownloadCorrelationId(ActivityExecutionContext context, HttpContext httpContext)
    {
        var downloadCorrelationId = DownloadCorrelationId.GetOrDefault(context);

        if (string.IsNullOrWhiteSpace(downloadCorrelationId))
            downloadCorrelationId = httpContext.Request.Headers["x-download-id"];

        if (string.IsNullOrWhiteSpace(downloadCorrelationId))
        {
            var identity = context.WorkflowExecutionContext.Workflow.Identity;
            var definitionId = identity.DefinitionId;
            var version = identity.Version.ToString();
            var correlationId = context.WorkflowExecutionContext.CorrelationId;
            var sources = new[] { definitionId, version, correlationId }.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

            downloadCorrelationId = string.Join("-", sources);
        }

        return downloadCorrelationId;
    }

    private async Task SendFileStream(ActivityExecutionContext context, HttpContext httpContext, Stream source, string contentType, string filename, EntityTagHeaderValue? eTag)
    {
        source.Seek(0, SeekOrigin.Begin);
        var enableResumableDownloads = EnableResumableDownloads.GetOrDefault(context, () => false);

        var result = new FileStreamResult(source, contentType)
        {
            EnableRangeProcessing = enableResumableDownloads,
            EntityTag = enableResumableDownloads ? eTag != null ? new Microsoft.Net.Http.Headers.EntityTagHeaderValue(eTag.ToString()) : default : default,
            FileDownloadName = filename
        };

        var actionContext = new ActionContext(httpContext, httpContext.GetRouteData(), new ActionDescriptor());
        await result.ExecuteResultAsync(actionContext);
    }

    private IEnumerable<Func<ValueTask<Downloadable>>> GetDownloadables(ActivityExecutionContext context, HttpContext httpContext, object? content)
    {
        if (content == null)
            return Enumerable.Empty<Func<ValueTask<Downloadable>>>();

        var manager = context.GetRequiredService<IDownloadableManager>();
        var headers = httpContext.Request.Headers;
        var eTag = GetIfMatchHeaderValue(headers);
        var range = GetRangeHeaderHeaderValue(headers);
        var options = new DownloadableOptions { ETag = eTag, Range = range };
        return manager.GetDownloadablesAsync(content, options, context.CancellationToken);
    }

    private string GetContentType(ActivityExecutionContext context, string filename)
    {
        var provider = context.GetRequiredService<IContentTypeProvider>();
        return provider.TryGetContentType(filename, out var contentType) ? contentType : System.Net.Mime.MediaTypeNames.Application.Octet;
    }
    
    private static RangeHeaderValue? GetRangeHeaderHeaderValue(IHeaderDictionary headers)
    {
        try
        {
            return headers.TryGetValue(HeaderNames.Range, out var header) ? RangeHeaderValue.Parse(header.ToString()) : default;
            
        }
        catch (Exception e)
        {
            throw new HttpBadRequestException("Failed to parse Range header value", e);
        }
    }
    
    private static EntityTagHeaderValue? GetIfMatchHeaderValue(IHeaderDictionary headers)
    {
        try
        {
            return headers.TryGetValue(HeaderNames.IfMatch, out var header) ? new EntityTagHeaderValue(header.ToString()) : default;
            
        }
        catch (Exception e)
        {
            throw new HttpBadRequestException("Failed to parse If-Match header value", e);
        }
    }

    private async ValueTask OnResumeAsync(ActivityExecutionContext context)
    {
        var httpContextAccessor = context.GetRequiredService<IHttpContextAccessor>();
        var httpContext = httpContextAccessor.HttpContext;

        if (httpContext == null)
            throw new FaultException("Cannot execute in a non-HTTP context");

        await WriteResponseAsync(context, httpContext);
    }
}