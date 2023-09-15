using Microsoft.AspNetCore.Http;

namespace Elsa.Http.Contracts;

/// <summary>
/// A general-purpose downloader of files from a given URL.
/// </summary>
public interface IFileDownloader
{
    /// <summary>
    /// Downloads a file from the specified URL.
    /// </summary>
    Task<HttpResponse> DownloadAsync(Uri url, CancellationToken cancellationToken = default);    
}