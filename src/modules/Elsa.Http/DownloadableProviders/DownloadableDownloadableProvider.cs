using Elsa.Http.Abstractions;
using Elsa.Http.Contexts;
using Elsa.Http.Models;

namespace Elsa.Http.DownloadableProviders;

/// <summary>
/// Handles content that represents a downloadable.
/// </summary>
public class DownloadableDownloadableProvider : DownloadableProviderBase
{
    /// <inheritdoc />
    public override bool GetSupportsContent(object content) => content is Downloadable;

    /// <inheritdoc />
    protected override Downloadable GetDownloadable(DownloadableContext context)
    {
        return (Downloadable) context.Content;
    }
}