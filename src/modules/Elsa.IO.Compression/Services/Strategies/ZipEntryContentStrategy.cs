using Elsa.IO.Compression.Common;
using Elsa.IO.Compression.Models;
using Elsa.IO.Contracts;
using Elsa.IO.Extensions;
using Elsa.IO.Models;
using Elsa.IO.Services.Strategies;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.IO.Compression.Services.Strategies;

/// <summary>
/// Strategy for resolving ZipEntry content with proper entry names.
/// </summary>
public class ZipEntryContentStrategy(IServiceProvider serviceProvider) : IContentResolverStrategy
{
    /// <inheritdoc />
    public float Priority => Constants.ZipEntryStrategyPriority;
    
    /// <inheritdoc />
    public bool CanResolve(object content) => content is ZipEntry;
    
    /// <inheritdoc />
    public async Task<BinaryContent> ResolveAsync(object content, CancellationToken cancellationToken = default)
    {
        var zipEntry = (ZipEntry)content;
        
        var resolver = serviceProvider.GetRequiredService<IContentResolver>();
        
        var innerContent = await resolver.ResolveAsync(zipEntry.Content, cancellationToken);

        if (string.IsNullOrEmpty(zipEntry.EntryName))
        {
            return innerContent;
        }

        var innerContentName = innerContent.Name?.GetNameAndExtension();
        var innerContentExtension = Path.GetExtension(innerContentName);
        innerContent.Name = !string.IsNullOrWhiteSpace(innerContentExtension) 
            ? Path.HasExtension(zipEntry.EntryName) ? zipEntry.EntryName : zipEntry.EntryName  + innerContentExtension 
            : innerContent.Name;

        return innerContent;
    }
}
