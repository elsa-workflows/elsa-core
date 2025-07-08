using Elsa.IO.Common;
using Elsa.IO.Extensions;
using Elsa.IO.Models;

namespace Elsa.IO.Services.Strategies;

/// <summary>
/// Strategy for handling base64 encoded content.
/// </summary>
public class Base64ContentStrategy : IContentResolverStrategy
{
    /// <inheritdoc />
    public float Priority => Constants.StrategyPriorities.Base64;

    /// <inheritdoc />
    public bool CanResolve(object content)
    {
        return content is string str && IsBase64String(str);
    }

    /// <inheritdoc />
    public Task<BinaryContent> ResolveAsync(object content, CancellationToken cancellationToken = default)
    {
        var str = content.ToString()!;
        var extension = ".bin";
        string? name = null;
        
        if (IsUriDataBase64String(str))
        {
            var dataUrlParts = str.Split(';');
            if (dataUrlParts.Length > 0 && dataUrlParts[0].StartsWith("data:"))
            {
                var contentType = dataUrlParts[0][5..];
                extension = contentType.GetExtensionFromContentType();
                
                name = "data" + extension;
            }
            
            str = str[(str.IndexOf("base64,", StringComparison.Ordinal) + 7)..];
        }
        
        var base64Bytes = Convert.FromBase64String(str);
        var stream = new MemoryStream(base64Bytes);
        
        return Task.FromResult(new BinaryContent
        {
            Name = name?.GetNameAndExtension(extension) ?? "data.bin",
            Stream = stream,
        });
    }

    private static bool IsBase64String(string base64)
    {
        return IsUriDataBase64String(base64)
               && base64.IsBase64String();
    }

    private static bool IsUriDataBase64String(string base64)
    {
        return base64.StartsWith("data:") && base64.Contains("base64");
    }
}