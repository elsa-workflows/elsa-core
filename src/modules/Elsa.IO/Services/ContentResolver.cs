using Elsa.IO.Contracts;
using Elsa.IO.Services.Strategies;

namespace Elsa.IO.Services;

/// <summary>
/// Resolves various content types to streams using a strategy pattern.
/// </summary>
public class ContentResolver : IContentResolver
{
    private readonly IEnumerable<IContentResolverStrategy> _strategies;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentResolver"/> class.
    /// </summary>
    public ContentResolver(IEnumerable<IContentResolverStrategy> strategies)
    {
        _strategies = strategies.OrderBy(s => s.Priority).ToList();
    }

    /// <inheritdoc />
    public async Task<Stream> ResolveContentAsync(object content, CancellationToken cancellationToken = default)
    {
        var strategy = _strategies.FirstOrDefault(s => s.CanResolve(content));

        if (strategy == null)
        {
            throw new ArgumentException($"Unsupported content type: {content.GetType().Name}");
        }

        return await strategy.ResolveAsync(content, cancellationToken);
    }
}