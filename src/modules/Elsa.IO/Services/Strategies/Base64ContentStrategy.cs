namespace Elsa.IO.Services.Strategies;

/// <summary>
/// Strategy for handling base64 encoded content.
/// </summary>
public class Base64ContentStrategy : IContentResolverStrategy
{
    /// <inheritdoc />
    public bool CanHandle(object content) => content is string str && str.StartsWith("base64:");

    /// <inheritdoc />
    public Task<Stream> ResolveAsync(object content, CancellationToken cancellationToken = default)
    {
        var str = (string)content;
        
        // Check if the string has the expected prefix before cutting
        var base64Content = str.StartsWith("base64:") && str.Length > 7 
            ? str.Substring(7) 
            : str; // Fallback to original string if no prefix
            
        var base64Bytes = Convert.FromBase64String(base64Content);
        var stream = new MemoryStream(base64Bytes);
        return Task.FromResult<Stream>(stream);
    }
}