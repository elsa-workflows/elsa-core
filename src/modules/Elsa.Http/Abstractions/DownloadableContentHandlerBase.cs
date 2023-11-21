using Elsa.Http.Contexts;
using Elsa.Http.Contracts;
using Elsa.Http.Models;

namespace Elsa.Http.Abstractions;

/// <summary>
/// Provides a base class for <see cref="IDownloadableContentHandler"/> implementations.
/// </summary>
public abstract class DownloadableContentHandlerBase : IDownloadableContentHandler
{
    /// <inheritdoc />
    public virtual float Priority => 0;

    /// <inheritdoc />
    public abstract bool GetSupportsContent(object content);

    /// <summary>
    /// Returns a list of downloadables from the specified content.
    /// </summary>
    protected virtual IEnumerable<Func<ValueTask<Downloadable>>> GetDownloadablesAsync(DownloadableContext context)
    {
        return new[] { GetDownloadableAsync(context) };
    }

    /// <summary>
    /// Returns a downloadable from the specified content.
    /// </summary>
    protected virtual Func<ValueTask<Downloadable>> GetDownloadableAsync(DownloadableContext context)
    {
        return () => ValueTask.FromResult(GetDownloadable(context));
    }
    
    /// <summary>
    /// Returns a downloadable from the specified content.
    /// </summary>
    /// <exception cref="NotImplementedException">This method is not implemented. The derived class must implement at least one of the GetDownloadable methods.</exception>
    protected virtual Downloadable GetDownloadable(DownloadableContext context)
    {
        throw new NotImplementedException();
    }

    IEnumerable<Func<ValueTask<Downloadable>>> IDownloadableContentHandler.GetDownloadablesAsync(DownloadableContext context)
    {
        return GetDownloadablesAsync(context);
    }
}