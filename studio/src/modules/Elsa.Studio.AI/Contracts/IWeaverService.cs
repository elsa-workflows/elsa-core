using Elsa.Studio.AI.Models;

namespace Elsa.Studio.AI.Contracts;

/// <summary>
/// Loads Weaver metadata and streams chat events from the active backend.
/// </summary>
public interface IWeaverService
{
    Task<WeaverCapabilities?> GetCapabilitiesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<WeaverToolDefinition>> GetToolsAsync(string? agent = default, CancellationToken cancellationToken = default);
    IAsyncEnumerable<WeaverStreamEvent> StreamChatAsync(WeaverChatRequest request, CancellationToken cancellationToken = default);
}
