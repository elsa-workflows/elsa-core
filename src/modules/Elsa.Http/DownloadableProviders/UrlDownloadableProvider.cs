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
        var response = await _fileDownloader.DownloadAsync(url, context.CancellationToken);
        
        // TODO: Get the filename from the response.
        var fileName = "";

        var stream = response.Body;
        var contentType = response.ContentType;
        
        return new Downloadable(stream, fileName, contentType);
    }
}