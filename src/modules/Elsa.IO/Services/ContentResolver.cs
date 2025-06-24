using Elsa.IO.Contracts;
using Elsa.IO.Services.Strategies;
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
    public async Task<Stream> ResolveContentAsync(object content, CancellationToken cancellationToken = default)
    {
        var strategy = _strategies.FirstOrDefault(s => s.CanHandle(content));
        
        if (strategy == null)
        {
            throw new ArgumentException($"Unsupported content type: {content?.GetType()?.Name ?? "null"}");
        }

        return await strategy.ResolveAsync(content, cancellationToken);
    }
}