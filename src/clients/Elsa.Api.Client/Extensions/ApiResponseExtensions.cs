using JetBrains.Annotations;
using Refit;

namespace Elsa.Api.Client.Extensions;

/// <summary>
/// Provides extension methods for <see cref="IApiResponse"/>.
/// </summary>
[UsedImplicitly]
public static class ApiResponseExtensions
{
    /// <summary>
    /// Returns the downloaded file name or the specified default file name.
    /// </summary>
    public static string GetDownloadedFileNameOrDefault(this IApiResponse response, string defaultFileName = "download.bin")
    {
        var fileName = defaultFileName;
        
        if (response.ContentHeaders is null)
            return fileName;

        if (!response.ContentHeaders.TryGetValues("content-disposition", out var contentDispositionHeader)) // Only available if the Elsa Server exposes the "Content-Disposition" header.
            return fileName;

        var contentDisposition = contentDispositionHeader.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(contentDisposition))
            return fileName;

        // Parse the Content-Disposition header value (e.g., "attachment; filename=workflow-definitions.zip; filename*=UTF-8''workflow-definitions.zip")
        // Split by semicolon and look for the filename parameter
        var parts = contentDisposition.Split(';', StringSplitOptions.RemoveEmptyEntries);
                
        foreach (var part in parts)
        {
            var trimmedPart = part.Trim();
                    
            // Look for filename= (prefer this over filename* for simplicity)
            if (!trimmedPart.StartsWith("filename=", StringComparison.OrdinalIgnoreCase) ||
                trimmedPart.StartsWith("filename*=", StringComparison.OrdinalIgnoreCase))
                continue;

            fileName = trimmedPart["filename=".Length..].Trim('"', '\'');
            break;
        }

        return fileName;
    }
}