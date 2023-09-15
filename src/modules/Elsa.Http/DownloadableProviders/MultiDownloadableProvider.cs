using System.Collections;
using Elsa.Http.Abstractions;
using Elsa.Http.Contexts;
using Elsa.Http.Models;

namespace Elsa.Http.DownloadableProviders;

/// <summary>
/// Handles content that represents a list of downloadable objects.
/// </summary>
public class MultiDownloadableProvider : DownloadableProviderBase
{
    /// <inheritdoc />
    public override bool GetSupportsContent(object content) => content is IEnumerable enumerable and not string;

    /// <inheritdoc />
    protected override async ValueTask<IEnumerable<Downloadable>> GetDownloadablesAsync(DownloadableContext context)
    {
        var collectedDownloadables = new List<Downloadable>();
        var content = context.Content;
        var enumerable = (IEnumerable) content;
        var manager = context.Manager;

        foreach (var item in enumerable)
        {
            var downloadables = await manager.GetDownloadablesAsync(item, context.CancellationToken);
            collectedDownloadables.AddRange(downloadables);
        }

        return collectedDownloadables;
    }
}