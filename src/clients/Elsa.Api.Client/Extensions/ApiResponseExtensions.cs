using Refit;

namespace Elsa.Api.Client.Extensions;

/// <summary>
/// Provides extension methods for <see cref="IApiResponse"/>.
/// </summary>
public static class ApiResponseExtensions
{
    /// <summary>
    /// Returns the downloaded file name or the specified default file name.
    /// </summary>
    public static string GetDownloadedFileNameOrDefault(this IApiResponse response, string defaultFileName = "download.bin")
    {
        var fileName = defaultFileName;

        if (response.Headers.TryGetValues("content-disposition", out var contentDispositionHeader)) // Only available if the Elsa Server exposes the "Content-Disposition" header.
        {
            // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
            var values = contentDispositionHeader.ToList() ?? [];

            if (values.Count >= 2)
                fileName = values[1].Split('=')[1];
        }

        return fileName;
    }
}