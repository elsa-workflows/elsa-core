using Elsa.IO.Common;

namespace Elsa.IO.Services.Strategies;

/// <summary>
/// Strategy for handling base64 encoded content.
/// </summary>
public class Base64ContentStrategy : IContentResolverStrategy
{
    public float Priority { get; init; } = Constants.StrategyPriorities.Base64;

    /// <inheritdoc />
    public bool CanResolve(object content)
    {
        return content is string str && IsBase64String(str);
    }

    /// <inheritdoc />
    public Task<Stream> ResolveAsync(object content, CancellationToken cancellationToken = default)
    {
        var str = content.ToString();

        if (IsUriDataBase64String(str!))
        {
            str = str![(str.IndexOf("base64,", StringComparison.Ordinal) + 7)..];
        }

        var base64Bytes = Convert.FromBase64String(str!);
        var stream = new MemoryStream(base64Bytes);
        return Task.FromResult<Stream>(stream);
    }

    private static bool IsBase64String(string base64)
    {
        if (IsUriDataBase64String(base64))
        {
            return true;
        }

        var buffer = new Span<byte>(new byte[base64.Length]);
        return Convert.TryFromBase64String(base64, buffer , out _);
    }

    private static bool IsUriDataBase64String(string base64)
    {
        return base64.StartsWith("data:") && base64.Contains("base64");
    }
}