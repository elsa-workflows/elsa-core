namespace Elsa.IO.Extensions;

public static class ContentTypeExtensions
{
    public static string GetExtensionFromContentType(this string? contentType)
    {
        if (string.IsNullOrEmpty(contentType))
            return ".bin";
            
        if (contentType.EndsWith("/pdf") || contentType == "application/pdf")
            return ".pdf";
            
        return contentType switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/gif" => ".gif",
            "image/svg+xml" => ".svg",
            "text/plain" => ".txt",
            "text/html" => ".html",
            "text/css" => ".css",
            "text/csv" => ".csv",
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
            _ => DetermineExtensionFromMimeType(contentType)
        };
    }

    public static string GetContentTypeFromExtension(this string filePath)
    {
        return filePath.GetFileExtension() switch
        {
            ".txt" => "text/plain",
            ".html" => "text/html",
            ".htm" => "text/html",
            ".css" => "text/css",
            ".js" => "application/javascript",
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".svg" => "image/svg+xml",
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".zip" => "application/zip",
            _ => "application/octet-stream"
        };
    }
    

    public static string GetFileExtension(this string filePath)
    {
        return Path.GetExtension(filePath).ToLowerInvariant();
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
        
        if (mimeType.StartsWith("file/") || mimeType.StartsWith("@file/"))
        {
            var extension = mimeType[(mimeType.IndexOf('/') + 1)..];
            if (!string.IsNullOrWhiteSpace(extension))
                return "." + extension;
        }
        
        return ".bin";
    }
}