namespace Elsa.IO.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
public static class ContentTypeExtensions
{
    private static readonly Dictionary<string, string> MimeMapping = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, HashSet<string>> ContentTypeToExtensionsMap = new(StringComparer.OrdinalIgnoreCase);
    
    static ContentTypeExtensions()
    {
        // Define all mappings in a single place
        AddMapping(".txt", "text/plain");
        AddMapping(".html", "text/html");
        AddMapping(".htm", "text/html");
        AddMapping(".css", "text/css");
        AddMapping(".js", "application/javascript");
        AddMapping(".json", "application/json");
        AddMapping(".xml", "application/xml");
        AddMapping(".jpg", "image/jpeg");
        AddMapping(".jpeg", "image/jpeg");
        AddMapping(".png", "image/png");
        AddMapping(".gif", "image/gif");
        AddMapping(".svg", "image/svg+xml");
        AddMapping(".pdf", "application/pdf");
        AddMapping(".doc", "application/msword");
        AddMapping(".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        AddMapping(".xls", "application/vnd.ms-excel");
        AddMapping(".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        AddMapping(".ppt", "application/vnd.ms-powerpoint");
        AddMapping(".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation");
        AddMapping(".zip", "application/zip");
        AddMapping(".csv", "text/csv");
    }
    private static void AddMapping(string extension, string contentType)
    {
        // Map extension to content type
        MimeMapping[extension] = contentType;
        // Map content type to extension(s)
        if (!ContentTypeToExtensionsMap.TryGetValue(contentType, out var extensions))
        {
            extensions = new(StringComparer.OrdinalIgnoreCase);
            ContentTypeToExtensionsMap[contentType] = extensions;
        }
        
        extensions.Add(extension);
    }
    public static string GetExtensionFromContentType(this string? contentType)
    {
        if (string.IsNullOrEmpty(contentType))
            return ".bin";
            
        if (contentType.EndsWith("/pdf") || contentType == "application/pdf")
            return ".pdf";
        
        if (ContentTypeToExtensionsMap.TryGetValue(contentType, out var extensions) && extensions.Any())
        {
            // Return the first extension for this content type
            return extensions.First();
        }
        
        return DetermineExtensionFromMimeType(contentType);
    }
    public static string GetContentTypeFromExtension(this string filePath)
    {
        var extension = filePath.GetFileExtension();
        
        return MimeMapping.GetValueOrDefault(extension, "application/octet-stream");
    }
    
    public static string GetNameAndExtension(this string fileName, string? extension = ".bin")
    {
        var currentExtension = fileName.GetFileExtension();
        if (!string.IsNullOrWhiteSpace(currentExtension))
        {
            return fileName;
        }

        return fileName + extension;
    }
    
    public static string GetFileExtension(this string filePath)
    {
        return Path.GetExtension(filePath).ToLowerInvariant();
    }
    
    public static bool IsBase64String(this string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return false;

        s = s.Trim();

        // Length must be divisible by 4
        if (s.Length % 4 != 0)
            return false;

        // Check padding position and count
        var paddingIndex = s.IndexOf('=');
        
        switch (paddingIndex)
        {
            // Padding cannot be at index 0
            case <= 0:
            // Padding must be at the end
            case > 0 when paddingIndex < s.Length - 2:
            // All characters after first '=' must also be '='
            case > 0 when s[paddingIndex..].Any(c => c != '='):
                return false;
        }

        // Check for valid Base64 characters
        for (var i = 0; i < paddingIndex; i++)
        {
            var c = s[i];
            var isValid = 
                c is >= 'A' and <= 'Z' ||
                c is >= 'a' and <= 'z' ||
                c is >= '0' and <= '9' ||
                c == '+' || c == '/';

            if (!isValid)
                return false;
        }

        // Additional check for short strings that are just lowercase+numbers
        // This catches "whatever" and similar false positives
        if (s.Length <= 10 && s.All(c => char.IsLower(c) || char.IsDigit(c)))
            return false;

        // Try actual decoding
        try
        {
            _ = Convert.FromBase64String(s);
            return true;
        }
        catch
        {
            return false;
        }
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