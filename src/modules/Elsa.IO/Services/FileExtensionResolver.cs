using Elsa.IO.Contracts;

namespace Elsa.IO.Services;

/// <summary>
/// Resolves appropriate file extensions for various content types.
/// </summary>
public class FileExtensionResolver : IFileExtensionResolver
{
    /// <summary>
    /// Ensures that a filename has an appropriate file extension based on its content type.
    /// </summary>
    public string EnsureFileExtension(string filename, object content)
    {
        if (Path.HasExtension(filename))
            return filename;

        var extension = content switch
        {
            byte[] => ".bin",
            Stream => ".bin",
            string str when str.StartsWith("data:") && str.Contains("base64") => GetExtensionFromDataUrl(str),
            string str when str.StartsWith("http://") || str.StartsWith("https://") => ".bin",
            string str when File.Exists(str) => Path.GetExtension(str) != string.Empty ? Path.GetExtension(str) : ".txt",
            string => ".txt",
            _ => ".bin"
        };

        return filename + extension;
    }

    private string GetExtensionFromDataUrl(string dataUrl)
    {
        try {
            if (dataUrl.Contains(';'))
            {
                // This handles standard data:mime/type;base64, format
                var start = dataUrl.IndexOf("data:", StringComparison.Ordinal) + 5;
                var end = dataUrl.IndexOf(';', start);
                if (start >= 5 && end > start) 
                {
                    var mimeType = dataUrl.Substring(start, end - start);

                    // Special case for PDFs where mime might be @file/pdf or application/pdf
                    if (mimeType.EndsWith("/pdf") || mimeType == "application/pdf")
                    {
                        return ".pdf";
                    }

                    return mimeType switch
                    {
                        "image/jpeg" => ".jpg",
                        "image/png" => ".png",
                        "image/gif" => ".gif",
                        "image/svg+xml" => ".svg",
                        "text/plain" => ".txt",
                        "text/html" => ".html",
                        "text/css" => ".css",
                        "text/csv" => ".csv",
                        "application/pdf" => ".pdf",
                        "application/json" => ".json",
                        "application/xml" => ".xml",
                        "application/zip" => ".zip",
                        "application/javascript" => ".js",
                        "application/vnd.ms-excel" => ".xls",
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => ".xlsx",
                        "application/vnd.ms-powerpoint" => ".ppt",
                        "application/vnd.openxmlformats-officedocument.presentationml.presentation" => ".pptx",
                        "application/msword" => ".doc",
                        "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => ".docx",
                        _ => DetermineExtensionFromMimeType(mimeType)
                    };
                }
            }
        }
        catch (Exception)
        {
            // If any parsing error occurs, we default to binary
            return ".bin";
        }

        return ".bin";
    }

    private static string DetermineExtensionFromMimeType(string mimeType)
    {
        if (mimeType.Contains("/pdf"))
            return ".pdf";

        if (mimeType.Contains("image/"))
            return ".img";

        if (mimeType.Contains("text/"))
            return ".txt";

        if (mimeType.Contains("audio/"))
            return ".audio";

        if (mimeType.Contains("video/"))
            return ".video";

        if (!mimeType.StartsWith("file/") && !mimeType.StartsWith("@file/"))
        {
            return ".bin";
        }

        var extension = mimeType[(mimeType.IndexOf('/') + 1)..];
        if (!string.IsNullOrWhiteSpace(extension))
            return "." + extension;

        return ".bin";
    }
}