using Elsa.Features.Services;

namespace Elsa.DropIns.Core;

/// <summary>
/// Implement this in your drop-in module to configure elsa and register services.
/// </summary>
public interface IDropIn
{
    /// <summary>
    /// Called when the drop-in is installed.
    /// </summary>
    /// <param name="module">The Elsa module.</param>
    void Install(IModule module);
    
    /// <summary>
    /// Called when the drop-in is being configured.
    /// </summary>
    ValueTask ConfigureAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken);
}