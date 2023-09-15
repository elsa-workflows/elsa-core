using Elsa.Http.Contexts;
using Elsa.Http.Contracts;
using Elsa.Http.Models;

namespace Elsa.Http.Services;

/// <inheritdoc />
public class DefaultDownloadableManager : IDownloadableManager
{
    private readonly IEnumerable<IDownloadableProvider> _providers;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultDownloadableManager"/> class.
    /// </summary>
    public DefaultDownloadableManager(IEnumerable<IDownloadableProvider> providers)
    {
        _providers = providers.OrderBy(x => x.Priority).ToList();
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<Downloadable>> GetDownloadablesAsync(object content, CancellationToken cancellationToken = default)
    {
        var provider = _providers.FirstOrDefault(x => x.GetSupportsContent(content));
        
        if (provider == null)
            return Enumerable.Empty<Downloadable>();
        
        var context = new DownloadableContext(this, content, cancellationToken);
        var downloadables = await provider.GetDownloadablesAsync(context);
        
        return downloadables;
    }
}