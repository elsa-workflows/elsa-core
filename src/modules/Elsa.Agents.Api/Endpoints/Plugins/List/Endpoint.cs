using Elsa.Abstractions;
using Elsa.Models;
using JetBrains.Annotations;

namespace Elsa.Agents.Api.Endpoints.Plugins.List;

/// <summary>
/// Lists all registered plugins.
/// </summary>
[UsedImplicitly]
public class Endpoint(IPluginDiscoverer pluginDiscoverer) : ElsaEndpointWithoutRequest<ListResponse<PluginDescriptorModel>>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Get("/ai/plugins");
        ConfigurePermissions("ai/plugins:read");
    }

    /// <inheritdoc />
    public override Task<ListResponse<PluginDescriptorModel>> ExecuteAsync(CancellationToken ct)
    {
        var descriptors = pluginDiscoverer.GetPluginDescriptors();
        var models = descriptors.Select(x => x.ToModel()).ToList();
        return Task.FromResult(new ListResponse<PluginDescriptorModel>(models));
    }
}