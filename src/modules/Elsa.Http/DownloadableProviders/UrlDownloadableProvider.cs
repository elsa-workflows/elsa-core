using System.Text.RegularExpressions;
using Elsa.Http.Abstractions;
using Elsa.Http.Contexts;
using Elsa.Http.Contracts;
using Elsa.Http.Models;

namespace Elsa.Http.DownloadableProviders;

/// <summary>
/// Handles content that represents a downloadable URL.
/// </summary>
public class UrlDownloadableProvider : DownloadableProviderBase
{
    private readonly IFileDownloader _fileDownloader;

    /// <inheritdoc />
    public UrlDownloadableProvider(IFileDownloader fileDownloader)
    {
        _fileDownloader = fileDownloader;
    }
    
    /// <inheritdoc />
    public override bool GetSupportsContent(object content) => (content is string url && url.StartsWith("http", StringComparison.OrdinalIgnoreCase)) || content is Uri;

    /// <inheritdoc />
    protected override async ValueTask<Downloadable> GetDownloadableAsync(DownloadableContext context)
    {
        var url = context.Content is string s ? new Uri(s) : (Uri)context.Content;
        var cancellationToken = context.CancellationToken;
        var response = await _fileDownloader.DownloadAsync(url, cancellationToken);
        var fileName = GetFilename(response) ?? url.Segments.Last();
        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        
        return new Downloadable(stream, fileName, contentType);
    }

    private static string? GetFilename(HttpResponseMessage response)
    {
        if (!response.Content.Headers.TryGetValues("Content-Disposition", out var values)) 
            return null;
        
        var contentDisposition = string.Join("", values);
        var match = Regex.Match(contentDisposition, """filename="?(?<filename>[^";]*)"?""");

        return match.Success ? match.Groups["filename"].Value : null;
    }

}