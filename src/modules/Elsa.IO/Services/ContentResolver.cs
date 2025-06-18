using Microsoft.Extensions.Logging;

namespace Elsa.IO.Services;

/// <summary>
/// Resolves various content types to streams using a strategy pattern.
/// </summary>
public class ContentResolver : IContentResolver
{
    private readonly IEnumerable<IContentResolverStrategy> _strategies;
    private readonly ILogger<ContentResolver> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentResolver"/> class.
    /// </summary>
    public ContentResolver(IEnumerable<IContentResolverStrategy> strategies, ILogger<ContentResolver> logger)
    {
        _strategies = strategies;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<(Stream Stream, string Name)> ResolveContentAsync(object content, string? name = null, CancellationToken cancellationToken = default)
    {
        var strategy = _strategies.FirstOrDefault(s => s.CanHandle(content));
        
        if (strategy == null)
        {
            throw new ArgumentException($"Unsupported content type: {content?.GetType()?.Name ?? "null"}");
        }

        var stream = await strategy.ResolveAsync(content, cancellationToken);
        var resolvedName = GetNameForContent(content, name);
        
        return (stream, resolvedName);
    }

    private static string GetNameForContent(object content, string? providedName)
    {
        if (!string.IsNullOrEmpty(providedName))
            return providedName;

        return content switch
        {
            string str when str.StartsWith("base64:") => "file.bin",
            string str when File.Exists(str) => Path.GetFileName(str),
            string str when str.StartsWith("http://") || str.StartsWith("https://") => GetFileNameFromUrl(str) ?? "downloaded-file.bin",
            string => "file.txt",
            byte[] => "file.bin",
            _ => "file.bin"
        };
    }

    private static string? GetFileNameFromUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            var segments = uri.Segments;
            var lastSegment = segments.LastOrDefault();
            return string.IsNullOrEmpty(lastSegment) || lastSegment == "/" ? null : lastSegment;
        }
        catch
        {
            return null;
        }
    }
}