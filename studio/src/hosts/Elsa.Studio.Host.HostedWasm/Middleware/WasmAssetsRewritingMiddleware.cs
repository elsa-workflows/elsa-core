namespace Elsa.Studio.Host.HostedWasm.Middleware;

/// <summary>
/// Middleware that intercepts HTTP requests and rewrites specific asset-related paths
/// for Web Assembly applications to ensure proper routing and handling of static files.
/// </summary>
public class WasmAssetsRewritingMiddleware(RequestDelegate next, ILogger<WasmAssetsRewritingMiddleware> logger)
{
    private static readonly string[] TargetSegments = { "_content", "_framework" };

    /// <summary>
    /// Invokes the handler asynchronously.
    /// </summary>
    /// <param name="context">The context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.HasValue && NeedsRewriting(context.Request.Path.Value!))
        {
            var path = context.Request.Path.Value!;
        
            // Call the private method to get the index of the target segment using spans
            var segmentIndex = GetTargetSegmentIndex(path.AsSpan());
        
            if (segmentIndex > 0)
            {
                // Use slices instead of Substring to avoid allocations
                var newPath = path.AsSpan(segmentIndex).ToString(); // Convert back to a string only when required
                context.Request.Path = newPath.StartsWith('/') ? newPath : $"/{newPath}";

                if (logger.IsEnabled(LogLevel.Debug)) logger.LogDebug("Rewritten: {Path} -> {NewPath}", path, context.Request.Path.Value);
            }
        }

        // Call the next middleware in the pipeline
        await next(context);
    }

    private static bool NeedsRewriting(string path)
    {
        // Fast path: Return if the path does not include potential target segments
        foreach (var segment in TargetSegments)
        {
            if (path.Contains(segment, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
    
    private static int GetTargetSegmentIndex(ReadOnlySpan<char> path)
    {
        foreach (var segment in TargetSegments)
        {
            // Use Span instead of string concatenation and index search
            var segmentAsSpan = $"{segment}/".AsSpan();
            var index = path.IndexOf(segmentAsSpan, StringComparison.OrdinalIgnoreCase);

            if (index >= 0)
            {
                return index;
            }
        }

        return -1; // No valid index found
    }

}