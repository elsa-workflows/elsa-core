using Elsa.Http.Abstractions;
using Elsa.Http.Contexts;
using Elsa.Http.Contracts;
using Elsa.Http.Models;
using Microsoft.AspNetCore.StaticFiles;

namespace Elsa.Http.DownloadableContentHandlers;

/// <summary>
/// Handles content that represents a downloadable URL.
/// </summary>
public class UrlDownloadableContentHandler : DownloadableContentHandlerBase
{
    private readonly IFileDownloader _fileDownloader;
    private readonly IContentTypeProvider _contentTypeProvider;

    /// <inheritdoc />
    public UrlDownloadableContentHandler(IFileDownloader fileDownloader, IContentTypeProvider contentTypeProvider)
    {
        _fileDownloader = fileDownloader;
        _contentTypeProvider = contentTypeProvider;
    }
    
    /// <inheritdoc />
    public override bool GetSupportsContent(object content) => (content is string url && url.StartsWith("http", StringComparison.OrdinalIgnoreCase)) || content is Uri;

    /// <inheritdoc />
    protected override async ValueTask<Downloadable> GetDownloadableAsync(DownloadableContext context)
    {
        var url = context.Content is string s ? new Uri(s) : (Uri)context.Content;
        var cancellationToken = context.CancellationToken;
        var response = await _fileDownloader.DownloadAsync(url, cancellationToken);
        var filename = GetFilename(response) ?? url.Segments.Last();
        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var contentType = response.Content.Headers.ContentType?.MediaType ?? GetContentType(filename);
        
        return new Downloadable(stream, filename, contentType);
    }

    private static string? GetFilename(HttpResponseMessage response)
    {
        if (!response.Content.Headers.TryGetValues("Content-Disposition", out var values)) 
            return null;
        
        var contentDispositionString = string.Join("", values);
        var contentDisposition = new System.Net.Mime.ContentDisposition(contentDispositionString);
        return contentDisposition.FileName;
    }
    
    private string GetContentType(string filename)
    {
        return _contentTypeProvider.TryGetContentType(filename, out var contentType) ? contentType : System.Net.Mime.MediaTypeNames.Application.Octet;
    }
}