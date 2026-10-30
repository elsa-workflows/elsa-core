using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Options;
using Microsoft.Extensions.Options;

namespace Elsa.ExternalAuthentication.Services;

/// <summary>
/// Provides the startup-installed adapter catalog without coupling callers to a concrete adapter.
/// </summary>
public sealed class DefaultExternalAuthenticationAdapterRegistry : IExternalAuthenticationAdapterRegistry
{
    private readonly IReadOnlyDictionary<string, IExternalAuthenticationAdapter> _adapters;
    private readonly IReadOnlyCollection<ExternalAuthenticationAdapterDescriptor> _descriptors;

    public DefaultExternalAuthenticationAdapterRegistry(
        IEnumerable<IExternalAuthenticationAdapter> adapters,
        ExtensionDescriptorValidator descriptorValidator,
        IOptions<ExternalAuthenticationOptions> options)
    {
        _adapters = ExtensionRegistryBuilder.Create(
            adapters,
            adapter => adapter.Type,
            options.Value.AllowedAdapterTypes,
            "adapter");
        _descriptors = _adapters.Values
            .Select(descriptorValidator.Validate)
            .OrderBy(x => x.Type, StringComparer.Ordinal)
            .ToArray();
    }

    public IReadOnlyCollection<ExternalAuthenticationAdapterDescriptor> ListDescriptors() => _descriptors;

    public bool TryGet(string type, out IExternalAuthenticationAdapter adapter) => _adapters.TryGetValue(type, out adapter!);
}
