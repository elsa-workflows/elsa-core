using Elsa.Studio.AI.Models;
using Refit;

namespace Elsa.Studio.AI.Client;

/// <summary>
/// Backend API for Weaver discovery and metadata.
/// </summary>
public interface IWeaverApi
{
    [Get("/ai/capabilities")]
    Task<WeaverCapabilities> GetCapabilitiesAsync(CancellationToken cancellationToken = default);

    [Get("/ai/tools")]
    Task<IReadOnlyCollection<WeaverToolDefinition>> GetToolsAsync(string? agent = default, CancellationToken cancellationToken = default);
}
